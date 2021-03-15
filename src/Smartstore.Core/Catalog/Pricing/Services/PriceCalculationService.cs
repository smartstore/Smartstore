using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Stores;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculationService : IPriceCalculationService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICommonServices _services;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IDiscountService _discountService;
        private readonly CatalogSettings _catalogSettings;
        private readonly Currency _primaryCurrency;
        private readonly Currency _workingCurrency;

        public PriceCalculationService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICommonServices services,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IDiscountService discountService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _services = services;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _currencyService = currencyService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _discountService = discountService;
            _catalogSettings = catalogSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual Money? GetSpecialPrice(Product product)
        {
            var amount = GetSpecialPriceAmount(product);

            return amount.HasValue ? new(amount.Value, _primaryCurrency) : null;
        }

        public virtual async Task<Money> GetProductCostAsync(Product product, ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var productCost = product.ProductCost;
            var attributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(selection);

            var productLinkageValues = attributeValues
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .ToList();
            var linkedProductIds = productLinkageValues
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            if (linkedProductIds.Any())
            {
                var linkedProducts = await _db.Products
                    .AsNoTracking()
                    .Where(x => linkedProductIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.ProductCost })
                    .ToListAsync();
                var linkedProductsDic = linkedProducts.ToDictionarySafe(x => x.Id, x => x.ProductCost);

                foreach (var value in productLinkageValues)
                {
                    if (linkedProductsDic.TryGetValue(value.LinkedProductId, out var linkedProductCost))
                    {
                        productCost += linkedProductCost * value.Quantity;
                    }
                }
            }

            return new(productCost, _primaryCurrency);
        }

        public virtual async Task<Money> GetPreselectedPriceAsync(Product product, Customer customer, ProductBatchContext context)
        {
            Guard.NotNull(product, nameof(product));

            var result = decimal.Zero;

            context ??= new ProductBatchContext(null, _services, _storeContext.CurrentStore, customer ?? _workContext.CurrentCustomer, true);

            if (product.ProductType == ProductType.BundledProduct)
            {
                var bundleItems = await context.ProductBundleItems.GetOrLoadAsync(product.Id);
                var bundleItemsData = bundleItems.Select(x => new ProductBundleItemData(x)).ToList();

                var productIds = bundleItemsData.Select(x => x.Item.ProductId).ToList();
                productIds.Add(product.Id);
                context.Collect(productIds);

                // Fetch bundleItemsData.AdditionalCharge for all bundle items.
                foreach (var bundleItem in bundleItemsData.Where(x => x.Item.Product.CanBeBundleItem()))
                {
                    var _ = await GetPreselectedPriceAmountAsync(bundleItem.Item.Product, customer, context, bundleItem, bundleItemsData);
                }

                result = await GetPreselectedPriceAmountAsync(product, customer, context, null, bundleItemsData);
            }
            else
            {
                result = await GetPreselectedPriceAmountAsync(product, customer, context, null, null);
            }

            return new(result, _primaryCurrency);
        }

        public virtual async Task<(Money LowestPrice, bool DisplayFromMessage)> GetLowestPriceAsync(Product product, Customer customer, ProductBatchContext context)
        {
            Guard.NotNull(product, nameof(product));

            if (product.ProductType == ProductType.GroupedProduct)
            {
                throw Error.InvalidOperation("Choose the other override for products of type grouped product.");
            }

            // Note, attribute price adjustments will not be taken into account here.
            context ??= new ProductBatchContext(null, _services, _storeContext.CurrentStore, customer ?? _workContext.CurrentCustomer, true);

            var isBundlePerItemPricing = product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing;
            var lowestPrice = await GetFinalPriceAmountAsync(product, null, decimal.Zero, customer, true, int.MaxValue, null, context);
            var displayFromMessage = isBundlePerItemPricing;

            if (product.LowestAttributeCombinationPrice.HasValue)
            {
                if (product.LowestAttributeCombinationPrice.Value < lowestPrice)
                {
                    lowestPrice = product.LowestAttributeCombinationPrice.Value;
                }
                displayFromMessage = true;
            }

            if (lowestPrice == decimal.Zero && product.Price == decimal.Zero)
            {
                lowestPrice = product.LowestAttributeCombinationPrice ?? decimal.Zero;
            }

            if (!displayFromMessage && product.ProductType != ProductType.BundledProduct)
            {
                var attributes = await context.Attributes.GetOrLoadAsync(product.Id);
                displayFromMessage = attributes.Any(x => x.ProductVariantAttributeValues.Any(y => y.PriceAdjustment != decimal.Zero));
            }

            if (!displayFromMessage && product.HasTierPrices && !isBundlePerItemPricing)
            {
                var allTierPrices = await context.TierPrices.GetOrLoadAsync(product.Id);
                var tierPrices = allTierPrices.RemoveDuplicatedQuantities();

                displayFromMessage = tierPrices.Count > 0 && !(tierPrices.Count == 1 && tierPrices.First().Quantity <= 1);
            }

            return (new(lowestPrice, _primaryCurrency), displayFromMessage);
        }

        public virtual async Task<(Money? LowestPrice, Product LowestPriceProduct)> GetLowestPriceAsync(
            Product product,
            Customer customer,
            ProductBatchContext context,
            IEnumerable<Product> associatedProducts)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(associatedProducts, nameof(associatedProducts));

            if (product.ProductType != ProductType.GroupedProduct)
            {
                throw Error.InvalidOperation("Choose the other override for products not of type grouped product.");
            }

            decimal? lowestPrice = null;
            Product lowestPriceProduct = null;

            context ??= new ProductBatchContext(null, _services, _storeContext.CurrentStore, customer ?? _workContext.CurrentCustomer, true);

            foreach (var associatedProduct in associatedProducts)
            {
                var tmpPrice = await GetFinalPriceAmountAsync(associatedProduct, decimal.Zero, customer, true, int.MaxValue, null, context);

                if (associatedProduct.LowestAttributeCombinationPrice.HasValue && associatedProduct.LowestAttributeCombinationPrice.Value < tmpPrice)
                {
                    tmpPrice = associatedProduct.LowestAttributeCombinationPrice.Value;
                }

                if (!lowestPrice.HasValue || tmpPrice < lowestPrice.Value)
                {
                    lowestPrice = tmpPrice;
                    lowestPriceProduct = associatedProduct;
                }
            }

            if (lowestPriceProduct == null)
            {
                lowestPriceProduct = associatedProducts.FirstOrDefault();
            }

            return (lowestPrice.HasValue ? new(lowestPrice.Value, _primaryCurrency) : null, lowestPriceProduct);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetFinalPriceAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            bool isTierPrice = false)
        {
            return new(await GetFinalPriceAmountAsync(
                product,
                additionalCharge?.Amount ?? decimal.Zero,
                customer,
                includeDiscounts,
                quantity,
                bundleItem,
                context,
                isTierPrice), _primaryCurrency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetFinalPriceAsync(
            Product product,
            IEnumerable<ProductBundleItemData> bundleItems,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null)
        {
            return new(await GetFinalPriceAmountAsync(
                product,
                bundleItems,
                additionalCharge?.Amount ?? decimal.Zero,
                customer,
                includeDiscounts,
                quantity,
                bundleItem,
                context), _primaryCurrency);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            Money? finalPrice = null)
        {
            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(
                product, 
                additionalCharge?.Amount ?? decimal.Zero, 
                customer, 
                quantity,
                bundleItem, 
                context, 
                finalPrice?.Amount);

            return (new(discountAmount, _primaryCurrency), appliedDiscount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetProductVariantAttributeValuePriceAdjustmentAsync(
            ProductVariantAttributeValue attributeValue,
            Product product,
            Customer customer,
            ProductBatchContext context,
            int quantity = 1)
        {
            return new(await GetVariantPriceAdjustmentAsync(attributeValue, product, customer, context, quantity), _primaryCurrency);
        }

        public virtual string GetBasePriceInfo(Product product, Money productPrice, Currency currency)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(currency, nameof(currency));

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var packageContentPerUnit = Math.Round(product.BasePriceAmount.Value, 2).ToString("G29");
                var basePrice = Convert.ToDecimal((productPrice / product.BasePriceAmount) * product.BasePriceBaseAmount);
                var basePriceAmount = _currencyService.ApplyTaxFormat(new Money(basePrice, currency));

                var result = T("Products.BasePriceInfo").Value.FormatInvariant(
                    packageContentPerUnit,
                    product.BasePriceMeasureUnit,
                    basePriceAmount,
                    product.BasePriceBaseAmount);

                return result;
            }

            return string.Empty;
        }

        public virtual async Task<string> GetBasePriceInfoAsync(Product product, Customer customer = null, Currency currency = null, Money? priceAdjustment = null)
        {
            Guard.NotNull(product, nameof(product));

            // Currency is only used for formatting, not for calculation.
            currency ??= _workContext.WorkingCurrency;
            customer ??= _workContext.CurrentCustomer;

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var currentPrice = await GetFinalPriceAmountAsync(product, decimal.Zero, customer, includeDiscounts: true);
                
                if (priceAdjustment.HasValue)
                {
                    currentPrice += priceAdjustment.Value.Amount;
                }

                var (price, _) = await _taxService.GetProductPriceAsync(product, new(currentPrice, _primaryCurrency), customer: customer);
                var convertedPrice = _currencyService.ConvertToCurrency(price, currency);

                return GetBasePriceInfo(product, convertedPrice, currency);
            }

            return string.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetUnitPriceAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts)
        {
            return new(await GetUnitPriceAmountAsync(shoppingCartItem, includeDiscounts), _primaryCurrency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetSubTotalAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts)
        {
            return new(await GetUnitPriceAmountAsync(shoppingCartItem, includeDiscounts) * shoppingCartItem.Item.Quantity, _primaryCurrency);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(OrganizedShoppingCartItem shoppingCartItem)
        {
            Guard.NotNull(shoppingCartItem, nameof(shoppingCartItem));

            var customer = shoppingCartItem.Item.Customer;
            var product = shoppingCartItem.Item.Product;
            var quantity = shoppingCartItem.Item.Quantity;

            if (product == null)
            {
                return (new(_primaryCurrency), null);
            }

            var attributesTotalPrice = decimal.Zero;
            var pvaValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(shoppingCartItem.Item.AttributeSelection);

            foreach (var pvaValue in pvaValues)
            {
                attributesTotalPrice += await GetVariantPriceAdjustmentAsync(pvaValue, product, customer, null, quantity);
            }

            var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(product, attributesTotalPrice, customer, quantity);
            discountAmount = _workingCurrency.RoundIfEnabledFor(discountAmount * quantity);

            return (new(discountAmount, _primaryCurrency), appliedDiscount);
        }

        #region Utilities

        private async Task<IEnumerable<TierPrice>> LoadTierPrices(Product product, Customer customer, ProductBatchContext context = null)
        {
            IEnumerable<TierPrice> result = null;

            if (product.HasTierPrices)
            {
                if (context == null)
                {
                    await _db.LoadCollectionAsync(product, x => x.TierPrices);

                    result = product.TierPrices
                        .FilterByStore(_storeContext.CurrentStore.Id)
                        .FilterForCustomer(customer)
                        .OrderBy(x => x.Quantity)
                        .ToList()
                        .RemoveDuplicatedQuantities();
                }
                else
                {
                    var tierPrices = await context.TierPrices.GetOrLoadAsync(product.Id);
                    result = tierPrices.RemoveDuplicatedQuantities();
                }
            }

            return result ?? Enumerable.Empty<TierPrice>();
        }

        protected virtual async Task<ICollection<Discount>> GetAllowedDiscountsAsync(Product product, Customer customer, ProductBatchContext context = null)
        {
            var result = new HashSet<Discount>();

            if (_catalogSettings.IgnoreDiscounts)
            {
                return result;
            }

            // Check discounts assigned to the product.
            // We use this property "HasDiscountsApplied" for performance optimziation to avoid unnecessary database calls.
            if (product.HasDiscountsApplied)
            {
                IEnumerable<Discount> appliedDiscounts;
                if (context == null)
                {
                    appliedDiscounts = product.AppliedDiscounts;
                }
                else
                {
                    if (!context.AppliedDiscounts.FullyLoaded)
                    {
                        context.AppliedDiscounts.LoadAll();
                    }
                    appliedDiscounts = await context.AppliedDiscounts.GetOrLoadAsync(product.Id);
                }

                if (appliedDiscounts != null)
                {
                    foreach (var discount in appliedDiscounts)
                    {
                        if (discount.DiscountType == DiscountType.AssignedToSkus && !result.Contains(discount) && await _discountService.IsDiscountValidAsync(discount, customer))
                        {
                            result.Add(discount);
                        }
                    }
                }
            }

            // Check discounts assigned to categories.
            var discountsAssignedToCategories = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToCategories);
            if (discountsAssignedToCategories?.Any() ?? false)
            {
                IEnumerable<ProductCategory> productCategories;
                if (context == null)
                {
                    productCategories = await _categoryService.GetProductCategoriesByProductIdsAsync(new[] { product.Id });
                }
                else
                {
                    productCategories = await context.ProductCategories.GetOrLoadAsync(product.Id);
                }

                if (productCategories?.Any() ?? false)
                {
                    foreach (var productCategory in productCategories)
                    {
                        var category = productCategory.Category;

                        if (category == null)
                            continue;

                        if (category.HasDiscountsApplied)
                        {
                            await _db.LoadCollectionAsync(category, x => x.AppliedDiscounts);
                            var categoryDiscounts = category.AppliedDiscounts;

                            foreach (var discount in categoryDiscounts)
                            {
                                if (discount.DiscountType == DiscountType.AssignedToCategories && !result.Contains(discount) && await _discountService.IsDiscountValidAsync(discount, customer))
                                {
                                    result.Add(discount);
                                }
                            }
                        }
                    }
                }
            }

            // Check discounts assigned to manufacturers.
            var discountsAssignedToManufacturers = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToManufacturers);
            if (discountsAssignedToManufacturers?.Any() ?? false)
            {
                IEnumerable<ProductManufacturer> productManufacturers = null;
                if (context == null)
                {
                    productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdsAsync(new[] { product.Id });
                }
                else
                {
                    productManufacturers = await context.ProductManufacturers.GetOrLoadAsync(product.Id);
                }

                if (productManufacturers?.Any() ?? false)
                {
                    foreach (var productManufacturer in productManufacturers)
                    {
                        var manu = productManufacturer.Manufacturer;
                        if (manu.HasDiscountsApplied)
                        {
                            await _db.LoadCollectionAsync(manu, x => x.AppliedDiscounts);
                            var manuDiscounts = manu.AppliedDiscounts;

                            foreach (var discount in manuDiscounts)
                            {
                                if (discount.DiscountType == DiscountType.AssignedToManufacturers && !result.Contains(discount) && await _discountService.IsDiscountValidAsync(discount, customer))
                                {
                                    result.Add(discount);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        protected virtual async Task<decimal?> GetMinimumTierPriceAsync(Product product, Customer customer, int quantity, ProductBatchContext context = null)
        {
            if (!product.HasTierPrices)
            {
                return decimal.Zero;
            }

            var previousQty = 1;
            decimal? result = null;
            var tierPrices = await LoadTierPrices(product, customer, context);

            foreach (var tierPrice in tierPrices)
            {
                if (quantity < tierPrice.Quantity || tierPrice.Quantity < previousQty)
                {
                    continue;
                }

                if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Fixed)
                {
                    result = tierPrice.Price;
                }
                else if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental)
                {
                    result = product.Price - (product.Price / 100m * tierPrice.Price);
                }
                else
                {
                    result = product.Price - tierPrice.Price;
                }

                previousQty = tierPrice.Quantity;
            }

            return result;
        }

        protected virtual async Task<decimal> GetTierPriceAttributeAdjustmentAsync(Product product, Customer customer, int quantity, ProductBatchContext context = null, decimal adjustment = 0)
        {
            var result = decimal.Zero;
            var tierPrices = await LoadTierPrices(product, customer, context);

            if (tierPrices.Any())
            {
                var previousQty = 1;

                foreach (var tierPrice in tierPrices)
                {
                    if (quantity < tierPrice.Quantity || tierPrice.Quantity < previousQty)
                    {
                        continue;
                    }

                    if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental && _catalogSettings.ApplyTierPricePercentageToAttributePriceAdjustments)
                    {
                        result = adjustment - (adjustment / 100m * tierPrice.Price);
                    }

                    previousQty = tierPrice.Quantity;
                }
            }

            return result;
        }

        protected virtual async Task<decimal> GetPreselectedPriceAmountAsync(
            Product product,
            Customer customer,
            ProductBatchContext context,
            ProductBundleItemData bundleItem,
            IEnumerable<ProductBundleItemData> bundleItems)
        {
            var attributesTotalPriceBase = decimal.Zero;
            var preSelectedPriceAdjustmentBase = decimal.Zero;
            var isBundle = product.ProductType == ProductType.BundledProduct;
            var isBundleItemPricing = bundleItem?.Item?.BundleProduct?.BundlePerItemPricing ?? false;
            var isBundlePricing = bundleItem != null && !bundleItem.Item.BundleProduct.BundlePerItemPricing;
            var bundleItemId = bundleItem?.Item?.Id ?? 0;

            var query = new ProductVariantQuery();
            var selectedAttributeValues = new List<ProductVariantAttributeValue>();
            var attributes = await context.Attributes.GetOrLoadAsync(product.Id);

            // 1. Fill query with initially selected attributes.
            foreach (var attribute in attributes.Where(x => x.ProductVariantAttributeValues.Any() && x.IsListTypeAttribute()))
            {
                await _db.LoadCollectionAsync(attribute, x => x.ProductVariantAttributeValues);

                var preSelectedValueId = 0;
                var selectedValueIds = new List<int>();
                ProductVariantAttributeValue defaultValue = null;
                var pvaValues = attribute.ProductVariantAttributeValues;

                foreach (var pvaValue in pvaValues)
                {
                    ProductBundleItemAttributeFilter attributeFilter = null;
                    if (bundleItem?.Item?.IsFilteredOut(pvaValue, out attributeFilter) ?? false)
                    {
                        continue;
                    }

                    if (preSelectedValueId == 0 && attributeFilter != null && attributeFilter.IsPreSelected)
                    {
                        preSelectedValueId = attributeFilter.AttributeValueId;
                    }

                    if (!isBundlePricing && pvaValue.IsPreSelected)
                    {
                        var attributeValuePriceAdjustment = await GetVariantPriceAdjustmentAsync(pvaValue, product, customer, context, 1);
                        // We cannot avoid money usage in calls between interfaces.
                        var (priceAdjustmentBase, _) = await _taxService.GetProductPriceAsync(product, new(attributeValuePriceAdjustment, _primaryCurrency), customer: customer);

                        preSelectedPriceAdjustmentBase += priceAdjustmentBase.Amount;
                    }
                }

                // Value pre-selected by a bundle item filter discards the default pre-selection.
                if (preSelectedValueId != 0 && (defaultValue = pvaValues.FirstOrDefault(x => x.Id == preSelectedValueId)) != null)
                {
                    //defaultValue.IsPreSelected = true;
                    selectedAttributeValues.Add(defaultValue);
                    query.AddVariant(new ProductVariantQueryItem(defaultValue.Id.ToString())
                    {
                        ProductId = product.Id,
                        BundleItemId = bundleItemId,
                        AttributeId = attribute.ProductAttributeId,
                        VariantAttributeId = attribute.Id,
                        Alias = attribute.ProductAttribute.Alias,
                        ValueAlias = defaultValue.Alias
                    });
                }
                else
                {
                    foreach (var value in pvaValues.Where(x => x.IsPreSelected))
                    {
                        selectedAttributeValues.Add(value);
                        query.AddVariant(new ProductVariantQueryItem(value.Id.ToString())
                        {
                            ProductId = product.Id,
                            BundleItemId = bundleItemId,
                            AttributeId = attribute.ProductAttributeId,
                            VariantAttributeId = attribute.Id,
                            Alias = attribute.ProductAttribute.Alias,
                            ValueAlias = value.Alias
                        });
                    }
                }
            }

            // 2. Find attribute combination for selected attributes and merge it.
            if (!isBundle && query.Variants.Any())
            {
                var (selection, warnings) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, attributes, product.Id, bundleItemId, true);
                var combinations = await context.AttributeCombinations.GetOrLoadAsync(product.Id);
                var selectedCombination = combinations.FirstOrDefault(x => x.AttributeSelection.Equals(selection));

                if (selectedCombination != null && selectedCombination.IsActive && selectedCombination.Price.HasValue)
                {
                    product.MergedDataValues = new Dictionary<string, object> { { "Price", selectedCombination.Price.Value } };

                    if (selectedCombination.BasePriceAmount.HasValue)
                        product.MergedDataValues.Add("BasePriceAmount", selectedCombination.BasePriceAmount.Value);

                    if (selectedCombination.BasePriceBaseAmount.HasValue)
                        product.MergedDataValues.Add("BasePriceBaseAmount", selectedCombination.BasePriceBaseAmount.Value);
                }
            }

            if (_catalogSettings.EnableDynamicPriceUpdate && !isBundlePricing)
            {
                if (selectedAttributeValues.Count > 0)
                {
                    foreach (var value in selectedAttributeValues)
                    {
                        attributesTotalPriceBase += await GetVariantPriceAdjustmentAsync(value, product, customer, context, 1);
                    }
                }
                else
                {
                    attributesTotalPriceBase = preSelectedPriceAdjustmentBase;
                }
            }

            if (bundleItem != null)
            {
                bundleItem.AdditionalCharge = new(attributesTotalPriceBase, _primaryCurrency);
            }

            var result = await GetFinalPriceAmountAsync(product, bundleItems, attributesTotalPriceBase, customer, true, 1, bundleItem, context);
            return result;
        }

        protected virtual decimal? GetSpecialPriceAmount(Product product)
        {
            Guard.NotNull(product, nameof(product));

            if (!product.SpecialPrice.HasValue)
            {
                return null;
            }

            var now = DateTime.UtcNow;

            if (product.SpecialPriceStartDateTimeUtc.HasValue)
            {
                var startDate = DateTime.SpecifyKind(product.SpecialPriceStartDateTimeUtc.Value, DateTimeKind.Utc);
                if (startDate.CompareTo(now) > 0)
                {
                    return null;
                }
            }
            if (product.SpecialPriceEndDateTimeUtc.HasValue)
            {
                var endDate = DateTime.SpecifyKind(product.SpecialPriceEndDateTimeUtc.Value, DateTimeKind.Utc);
                if (endDate.CompareTo(now) < 0)
                {
                    return null;
                }
            }

            return product.SpecialPrice.Value;
        }

        protected virtual async Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            decimal additionalCharge,
            Customer customer = null,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            decimal? finalPrice = null)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;

            var discountAmount = decimal.Zero;
            Discount appliedDiscount = null;

            if (bundleItem != null && bundleItem.Item != null)
            {
                var bi = bundleItem.Item;
                if (bi.Discount.HasValue && bi.BundleProduct.BundlePerItemPricing)
                {
                    appliedDiscount = new Discount
                    {
                        UsePercentage = bi.DiscountPercentage,
                        DiscountPercentage = bi.Discount.Value,
                        DiscountAmount = bi.Discount.Value
                    };

                    var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAmountAsync(product, additionalCharge, customer, false, quantity, bundleItem, context);
                    discountAmount = appliedDiscount.GetDiscountAmount(finalPriceWithoutDiscount);
                }
            }
            else
            {
                // Don't apply when customer entered price or discounts should be ignored in any case.
                if (!product.CustomerEntersPrice && !_catalogSettings.IgnoreDiscounts)
                {
                    var allowedDiscounts = await GetAllowedDiscountsAsync(product, customer, context);
                    if (allowedDiscounts.Any())
                    {
                        var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAmountAsync(product, additionalCharge, customer, false, quantity, bundleItem, context);
                        appliedDiscount = allowedDiscounts.GetPreferredDiscount(finalPriceWithoutDiscount);

                        if (appliedDiscount != null)
                        {
                            discountAmount = appliedDiscount.GetDiscountAmount(finalPriceWithoutDiscount);
                        }
                    }
                }
            }

            return (discountAmount, appliedDiscount);
        }

        protected virtual async Task<decimal> GetVariantPriceAdjustmentAsync(
            ProductVariantAttributeValue attributeValue,
            Product product,
            Customer customer,
            ProductBatchContext context,
            int quantity = 1)
        {
            Guard.NotNull(attributeValue, nameof(attributeValue));

            if (attributeValue.ValueType == ProductVariantAttributeValueType.Simple)
            {
                if (quantity > 1 && attributeValue.PriceAdjustment > decimal.Zero)
                {
                    var tierPriceAttributeAdjustment = await GetTierPriceAttributeAdjustmentAsync(product, customer, quantity, context, attributeValue.PriceAdjustment);
                    if (tierPriceAttributeAdjustment != decimal.Zero)
                    {
                        return tierPriceAttributeAdjustment;
                    }
                }

                return attributeValue.PriceAdjustment;
            }
            else if (attributeValue.ValueType == ProductVariantAttributeValueType.ProductLinkage)
            {
                var linkedProduct = await _db.Products.FindByIdAsync(attributeValue.LinkedProductId);
                if (linkedProduct != null)
                {
                    var productPrice = await GetFinalPriceAmountAsync(linkedProduct, decimal.Zero, includeDiscounts: true);
                    return productPrice * attributeValue.Quantity;
                }
            }

            return decimal.Zero;
        }

        protected virtual async Task<decimal> GetFinalPriceAmountAsync(
            Product product,
            decimal additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null,
            bool isTierPrice = false)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;

            if (isTierPrice)
            {
                includeDiscounts = true;
            }

            var result = GetSpecialPriceAmount(product) ?? product.Price;
 
            // Tier price.
            if (product.HasTierPrices && includeDiscounts && !(bundleItem != null && bundleItem.Item != null))
            {
                var tierPrice = await GetMinimumTierPriceAsync(product, customer, quantity, context);
                if (tierPrice.HasValue)
                {
                    if (_catalogSettings.ApplyPercentageDiscountOnTierPrice && !isTierPrice)
                    {
                        var (discountOnTierPrice, appliedDiscount) = await GetDiscountAmountAsync(product, decimal.Zero, customer, quantity, bundleItem, context, tierPrice.Value);

                        if (appliedDiscount != null && appliedDiscount.UsePercentage)
                        {
                            result = Math.Min(result, tierPrice.Value) + additionalCharge - discountOnTierPrice;

                            return Math.Max(result, decimal.Zero);
                        }
                    }

                    var (discountAmountTest, _) = await GetDiscountAmountAsync(product, additionalCharge, customer, quantity, bundleItem);
                    var discountProductTest = result - discountAmountTest;

                    if (tierPrice < discountProductTest)
                    {
                        includeDiscounts = false;
                        result = Math.Min(result, tierPrice.Value);
                    }
                }
            }

            result += additionalCharge;

            if (includeDiscounts)
            {
                var (discountAmount, _) = await GetDiscountAmountAsync(product, additionalCharge, customer, quantity, bundleItem, context);
                result -= discountAmount;
            }

            return Math.Max(result, decimal.Zero);
        }

        protected virtual async Task<decimal> GetFinalPriceAmountAsync(
            Product product,
            IEnumerable<ProductBundleItemData> bundleItems,
            decimal additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            ProductBatchContext context = null)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                var result = decimal.Zero;
                var items = bundleItems;

                if (items == null)
                {
                    var loadedBundleItems = context != null
                        ? await context.ProductBundleItems.GetOrLoadAsync(product.Id)
                        : await _db.ProductBundleItem
                            .AsNoTracking()
                            .Include(x => x.Product)
                            .ApplyBundledProductsFilter(new[] { product.Id })
                            .ToListAsync();

                    items = loadedBundleItems.Select(x => new ProductBundleItemData(x)).ToList();
                }

                foreach (var itemData in items.Where(x => x?.Item != null))
                {
                    var itemPrice = await GetFinalPriceAmountAsync(itemData.Item.Product, itemData.AdditionalCharge?.Amount ?? decimal.Zero, customer, includeDiscounts, 1, itemData, context);

                    result += itemPrice * itemData.Item.Quantity;
                }

                return Math.Max(result, decimal.Zero);
            }

            return await GetFinalPriceAmountAsync(product, additionalCharge, customer, includeDiscounts, quantity, bundleItem, context);
        }

        protected virtual async Task<decimal> GetUnitPriceAmountAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts)
        {
            Guard.NotNull(shoppingCartItem, nameof(shoppingCartItem));

            var finalPrice = decimal.Zero;
            var cartItem = shoppingCartItem.Item;
            var customer = cartItem.Customer;
            var product = cartItem.Product;

            if (product != null)
            {
                if (product.CustomerEntersPrice)
                {
                    finalPrice = cartItem.CustomerEnteredPrice;
                }
                else if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
                {
                    if (shoppingCartItem.ChildItems != null)
                    {
                        foreach (var bundleItem in shoppingCartItem.ChildItems)
                        {
                            await _productAttributeMaterializer.MergeWithCombinationAsync(bundleItem.Item.Product, bundleItem.Item.AttributeSelection);
                        }

                        var bundleItems = shoppingCartItem.ChildItems
                            .Where(x => x.BundleItemData?.Item != null)
                            .Select(x => x.BundleItemData);

                        finalPrice = await GetFinalPriceAmountAsync(product, bundleItems, decimal.Zero, customer, includeDiscounts, cartItem.Quantity);
                    }
                }
                else
                {
                    await _productAttributeMaterializer.MergeWithCombinationAsync(product, cartItem.AttributeSelection);

                    var pvaValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(cartItem.AttributeSelection);
                    var attributesTotalPrice = decimal.Zero;

                    foreach (var pvaValue in pvaValues)
                    {
                        attributesTotalPrice += await GetVariantPriceAdjustmentAsync(pvaValue, product, customer, null, cartItem.Quantity);
                    }

                    finalPrice = await GetFinalPriceAmountAsync(product, attributesTotalPrice, customer, includeDiscounts, cartItem.Quantity, shoppingCartItem.BundleItemData);
                }
            }

            return _workingCurrency.RoundIfEnabledFor(finalPrice);
        }

        #endregion
    }
}
