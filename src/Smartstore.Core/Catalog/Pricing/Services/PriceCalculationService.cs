using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
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
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IDiscountService _discountService;
        private readonly CatalogSettings _catalogSettings;

        public PriceCalculationService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
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
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _currencyService = currencyService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _discountService = discountService;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual PriceCalculationContext CreatePriceCalculationContext(
            IEnumerable<Product> products = null,
            Customer customer = null,
            int? storeId = null,
            bool includeHidden = true)
        {
            customer ??= _workContext.CurrentCustomer;
            storeId ??= _storeContext.CurrentStore.Id;

            async Task<Multimap<int, ProductVariantAttribute>> attributesFactory(int[] ids)
            {
                var attributes = await _db.ProductVariantAttributes
                    .AsNoTracking()
                    .Include(x => x.ProductAttribute)
                    .Include(x => x.ProductVariantAttributeValues)
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .ThenBy(x => x.DisplayOrder)
                    .ToListAsync();

                return attributes.ToMultimap(x => x.ProductId, x => x);
            }

            async Task<Multimap<int, ProductVariantAttributeCombination>> attributeCombinationsFactory(int[] ids)
            {
                var attributeCombinations = await _db.ProductVariantAttributeCombinations
                    .AsNoTracking()
                    .Where(x => ids.Contains(x.ProductId))
                    .OrderBy(x => x.ProductId)
                    .ToListAsync();

                return attributeCombinations.ToMultimap(x => x.ProductId, x => x);
            }

            async Task<Multimap<int, TierPrice>> tierPriceFactory(int[] ids)
            {
                var tierPrices = await _db.TierPrices
                    .AsNoTracking()
                    .Include(x => x.CustomerRole)
                    .Where(x => ids.Contains(x.ProductId) && (x.StoreId == 0 || x.StoreId == storeId.Value))
                    .ToListAsync();

                return tierPrices
                    // Sorting locally is most likely faster.
                    .OrderBy(x => x.ProductId)
                    .ThenBy(x => x.Quantity)
                    .FilterForCustomer(customer)
                    .ToMultimap(x => x.ProductId, x => x);
            }

            async Task<Multimap<int, ProductCategory>> productCategoriesFactory(int[] ids)
            {
                var productCategories = await _categoryService.GetProductCategoriesByProductIdsAsync(ids, includeHidden);
                return productCategories.ToMultimap(x => x.ProductId, x => x);
            }

            async Task<Multimap<int, ProductManufacturer>> productManufacturersFactory(int[] ids)
            {
                var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdsAsync(ids, includeHidden);
                return productManufacturers.ToMultimap(x => x.ProductId, x => x);
            }

            async Task<Multimap<int, Discount>> appliedDiscountsFactory(int[] ids)
            {
                var discounts = await _db.Products
                    .AsNoTracking()
                    .Include(x => x.AppliedDiscounts)
                        .ThenInclude(x => x.RuleSets)
                    .Where(x => ids.Contains(x.Id))
                    .Select(x => new
                    {
                        ProductId = x.Id,
                        Discounts = x.AppliedDiscounts
                    })
                    .ToListAsync();

                var map = new Multimap<int, Discount>();
                discounts.Each(x => map.AddRange(x.ProductId, x.Discounts));

                return map;
            }

            async Task<Multimap<int, ProductBundleItem>> productBundleItemsFactory(int[] ids)
            {
                var bundleItemsQuery = _db.ProductBundleItem
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.BundleProduct);

                var query =
                    from pbi in bundleItemsQuery
                    join p in _db.Products.AsNoTracking() on pbi.ProductId equals p.Id
                    where ids.Contains(pbi.BundleProductId) && (includeHidden || (pbi.Published && p.Published))
                    orderby pbi.DisplayOrder
                    select pbi;

                var bundleItems = await query.ToListAsync();

                return bundleItems.ToMultimap(x => x.BundleProductId, x => x);
            }

            async Task<Multimap<int, Product>> associatedProductsFactory(int[] ids)
            {
                var associatedProducts = await _db.Products
                    .AsNoTracking()
                    .ApplyAssociatedProductsFilter(ids, includeHidden)
                    .ToListAsync();

                return associatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
            }

            var context = new PriceCalculationContext(products)
            {
                AttributesFactory = attributesFactory,
                AttributeCombinationsFactory = attributeCombinationsFactory,
                TierPricesFactory = tierPriceFactory,
                ProductCategoriesFactory = productCategoriesFactory,
                ProductManufacturersFactory = productManufacturersFactory,
                AppliedDiscountsFactory = appliedDiscountsFactory,
                ProductBundleItemsFactory = productBundleItemsFactory,
                AssociatedProductsFactory = associatedProductsFactory
            };

            return context;
        }

        public virtual Money? GetSpecialPrice(Product product)
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

            return new Money(product.SpecialPrice.Value, _workContext.WorkingCurrency);
        }

        public virtual async Task<Money> GetProductCostAsync(Product product, ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var result = new Money(product.ProductCost, _workContext.WorkingCurrency);
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
                        result += linkedProductCost * value.Quantity;
                    }
                }
            }

            return result;
        }

        public virtual async Task<Money> GetPreselectedPriceAsync(Product product, Customer customer, Currency currency, PriceCalculationContext context)
        {
            Guard.NotNull(product, nameof(product));

            context ??= CreatePriceCalculationContext(customer: customer);

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
                    var _ = await GetPreselectedPriceAsync(bundleItem.Item.Product, customer, currency, context, bundleItem, bundleItemsData);
                }

                return await GetPreselectedPriceAsync(product, customer, currency, context, null, bundleItemsData);
            }
            else
            {
                return await GetPreselectedPriceAsync(product, customer, currency, context, null, null);
            }
        }

        public virtual async Task<(Money LowestPrice, bool DisplayFromMessage)> GetLowestPriceAsync(Product product, Customer customer, PriceCalculationContext context)
        {
            Guard.NotNull(product, nameof(product));

            if (product.ProductType == ProductType.GroupedProduct)
            {
                throw Error.InvalidOperation("Choose the other override for products of type grouped product.");
            }

            // Note, attribute price adjustments will not be taken into account here.
            context ??= CreatePriceCalculationContext(customer: customer);

            var isBundlePerItemPricing = product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing;
            var lowestPrice = await GetFinalPriceAsync(product, null, customer, true, int.MaxValue, null, context);
            var displayFromMessage = isBundlePerItemPricing;

            if (product.LowestAttributeCombinationPrice.HasValue)
            {
                if (product.LowestAttributeCombinationPrice.Value < lowestPrice.Amount)
                {
                    lowestPrice.Amount = product.LowestAttributeCombinationPrice.Value;
                }
                displayFromMessage = true;
            }

            if (lowestPrice == decimal.Zero && product.Price == decimal.Zero)
            {
                lowestPrice.Amount = product.LowestAttributeCombinationPrice ?? decimal.Zero;
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

            return (lowestPrice, displayFromMessage);
        }

        public virtual async Task<(Money? LowestPrice, Product LowestPriceProduct)> GetLowestPriceAsync(
            Product product,
            Customer customer,
            PriceCalculationContext context,
            IEnumerable<Product> associatedProducts)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(associatedProducts, nameof(associatedProducts));

            if (product.ProductType != ProductType.GroupedProduct)
            {
                throw Error.InvalidOperation("Choose the other override for products not of type grouped product.");
            }

            Money? lowestPrice = null;
            Product lowestPriceProduct = null;

            context ??= CreatePriceCalculationContext(customer: customer);

            foreach (var associatedProduct in associatedProducts)
            {
                var tmpPrice = await GetFinalPriceAsync(associatedProduct, null, customer, true, int.MaxValue, null, context);

                if (associatedProduct.LowestAttributeCombinationPrice.HasValue && associatedProduct.LowestAttributeCombinationPrice.Value < tmpPrice.Amount)
                {
                    tmpPrice = new Money(associatedProduct.LowestAttributeCombinationPrice.Value, _workContext.WorkingCurrency);
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

            return (lowestPrice, lowestPriceProduct);
        }

        public virtual async Task<Money> GetFinalPriceAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
            bool isTierPrice = false)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;

            if (isTierPrice)
            {
                includeDiscounts = true;
            }

            var currency = _workContext.WorkingCurrency;
            var specialPrice = GetSpecialPrice(product);

            var result = specialPrice.HasValue
                ? specialPrice.Value.Amount
                : product.Price;

            // Tier price.
            if (product.HasTierPrices && includeDiscounts && !(bundleItem != null && bundleItem.Item != null))
            {
                var tierPrice = await GetMinimumTierPriceAsync(product, customer, quantity, context);
                if (tierPrice.HasValue)
                {
                    if (_catalogSettings.ApplyPercentageDiscountOnTierPrice && !isTierPrice)
                    {
                        var (discountOnTierPrice, appliedDiscount) = await GetDiscountAmountAsync(product, null, customer, quantity, bundleItem, context, tierPrice.Value);

                        if (appliedDiscount != null && appliedDiscount.UsePercentage)
                        {
                            result = Math.Min(result, tierPrice.Value.Amount) + (additionalCharge?.Amount ?? decimal.Zero) - discountOnTierPrice.Amount;

                            return currency.AsMoney(result, false, true);
                        }
                    }

                    var (discountAmountTest, _) = await GetDiscountAmountAsync(product, additionalCharge, customer, quantity, bundleItem);
                    var discountProductTest = result - discountAmountTest.Amount;

                    if (tierPrice < discountProductTest)
                    {
                        includeDiscounts = false;
                        result = Math.Min(result, tierPrice.Value.Amount);
                    }
                }
            }

            // Discount + additional charge.
            result += additionalCharge?.Amount ?? decimal.Zero;

            if (includeDiscounts)
            {
                var (discountAmount, _) = await GetDiscountAmountAsync(product, additionalCharge, customer, quantity, bundleItem, context);
                result -= discountAmount.Amount;
            }

            return currency.AsMoney(result, false, true);
        }

        public virtual async Task<Money> GetFinalPriceAsync(
            Product product,
            IEnumerable<ProductBundleItemData> bundleItems,
            Money? additionalCharge,
            Customer customer = null,
            bool includeDiscounts = true,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null)
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
                            .ApplyBundledProductsFilter(new int[] { product.Id })
                            .ToListAsync();

                    items = loadedBundleItems.Select(x => new ProductBundleItemData(x)).ToList();
                }

                foreach (var itemData in items.Where(x => x?.Item != null))
                {
                    var itemPrice = await GetFinalPriceAsync(itemData.Item.Product, itemData.AdditionalCharge, customer, includeDiscounts, 1, itemData, context);

                    result += itemPrice.Amount * itemData.Item.Quantity;
                }

                return _workContext.WorkingCurrency.AsMoney(result, false, true);
            }

            return await GetFinalPriceAsync(product, additionalCharge, customer, includeDiscounts, quantity, bundleItem, context);
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Money? additionalCharge,
            Customer customer = null,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
            Money? finalPrice = null)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;

            var currency = finalPrice?.Currency ?? _workContext.WorkingCurrency;
            var discountAmount = new Money(currency);
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

                    var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAsync(product, additionalCharge, customer, false, quantity, bundleItem, context);
                    discountAmount = appliedDiscount.GetDiscountAmount(finalPriceWithoutDiscount);
                }
            }
            else
            {
                // Don't apply when customer entered price or discounts should be ignored in any case.
                if (!product.CustomerEntersPrice && _catalogSettings.IgnoreDiscounts)
                {
                    return (discountAmount, appliedDiscount);
                }

                var allowedDiscounts = await GetAllowedDiscountsAsync(product, customer, context);
                if (!allowedDiscounts.Any())
                {
                    return (discountAmount, appliedDiscount);
                }

                var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAsync(product, additionalCharge, customer, false, quantity, bundleItem, context);
                appliedDiscount = allowedDiscounts.GetPreferredDiscount(finalPriceWithoutDiscount);

                if (appliedDiscount != null)
                {
                    discountAmount = appliedDiscount.GetDiscountAmount(finalPriceWithoutDiscount);
                }
            }

            return (discountAmount, appliedDiscount);
        }

        public virtual async Task<Money> GetProductVariantAttributeValuePriceAdjustmentAsync(
            ProductVariantAttributeValue attributeValue,
            Product product,
            Customer customer,
            PriceCalculationContext context,
            int quantity = 1)
        {
            Guard.NotNull(attributeValue, nameof(attributeValue));

            var currency = _workContext.WorkingCurrency;

            if (attributeValue.ValueType == ProductVariantAttributeValueType.Simple)
            {
                if (quantity > 1 && attributeValue.PriceAdjustment > 0)
                {
                    var tierPriceAttributeAdjustment = await GetTierPriceAttributeAdjustmentAsync(product, customer, quantity, context, attributeValue.PriceAdjustment);
                    if (tierPriceAttributeAdjustment != 0)
                    {
                        return tierPriceAttributeAdjustment;
                    }
                }

                return new Money(attributeValue.PriceAdjustment, currency);
            }

            if (attributeValue.ValueType == ProductVariantAttributeValueType.ProductLinkage)
            {
                var linkedProduct = await _db.Products.FindByIdAsync(attributeValue.LinkedProductId);
                if (linkedProduct != null)
                {
                    var productPrice = await GetFinalPriceAsync(linkedProduct, null) * attributeValue.Quantity;
                    return productPrice;
                }
            }

            return new Money(currency);
        }

        public virtual string GetBasePriceInfo(Product product, Money productPrice, Currency currency)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(productPrice, nameof(productPrice));
            Guard.NotNull(currency, nameof(currency));

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var packageContentPerUnit = Math.Round(product.BasePriceAmount.Value, 2).ToString("G29");
                var basePrice = Convert.ToDecimal((productPrice / product.BasePriceAmount) * product.BasePriceBaseAmount);
                var basePriceFormatted = _currencyService.CreateMoney(basePrice, true, currency).ToString();

                var result = T("Products.BasePriceInfo").Value.FormatInvariant(
                    packageContentPerUnit,
                    product.BasePriceMeasureUnit,
                    basePriceFormatted,
                    product.BasePriceBaseAmount);

                return result;
            }

            return string.Empty;
        }

        public virtual async Task<string> GetBasePriceInfoAsync(Product product, Customer customer = null, Currency currency = null, Money? priceAdjustment = null)
        {
            Guard.NotNull(product, nameof(product));

            customer ??= _workContext.CurrentCustomer;
            currency ??= _workContext.WorkingCurrency;

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var currentPrice = await GetFinalPriceAsync(product, null, customer, includeDiscounts: true);
                
                if (priceAdjustment.HasValue)
                {
                    currentPrice += priceAdjustment.Value;
                }

                var (price, _) = await _taxService.GetProductPriceAsync(product, currentPrice, customer: customer);
                var convertedPrice = _currencyService.ConvertFromPrimaryStoreCurrency(price);

                return GetBasePriceInfo(product, convertedPrice, currency);
            }

            return string.Empty;
        }

        public virtual async Task<Money> GetUnitPriceAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts)
        {
            Guard.NotNull(shoppingCartItem, nameof(shoppingCartItem));

            var currency = _workContext.WorkingCurrency;
            var cartItem = shoppingCartItem.Item;
            var customer = cartItem.Customer;
            var product = cartItem.Product;

            if (product != null)
            {
                if (product.CustomerEntersPrice)
                {
                    return currency.AsMoney(cartItem.CustomerEnteredPrice);
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

                        var finalPrice = await GetFinalPriceAsync(product, bundleItems, null, customer, includeDiscounts, cartItem.Quantity);
                        return currency.AsMoney(finalPrice.Amount);
                    }
                }
                else
                {
                    await _productAttributeMaterializer.MergeWithCombinationAsync(product, cartItem.AttributeSelection);

                    var pvaValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(cartItem.AttributeSelection);
                    var attributesTotalPrice = new Money(currency);

                    foreach (var pvaValue in pvaValues)
                    {
                        attributesTotalPrice += await GetProductVariantAttributeValuePriceAdjustmentAsync(pvaValue, product, customer, null, cartItem.Quantity);
                    }

                    var finalPrice = await GetFinalPriceAsync(product, attributesTotalPrice, customer, includeDiscounts, cartItem.Quantity, shoppingCartItem.BundleItemData);
                    return currency.AsMoney(finalPrice.Amount);
                }
            }

            return new Money(currency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<Money> GetSubTotalAsync(OrganizedShoppingCartItem shoppingCartItem, bool includeDiscounts)
        {
            return await GetUnitPriceAsync(shoppingCartItem, includeDiscounts) * shoppingCartItem.Item.Quantity;
        }

        public virtual async Task<(Money Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(OrganizedShoppingCartItem shoppingCartItem)
        {
            Guard.NotNull(shoppingCartItem, nameof(shoppingCartItem));

            var currency = _workContext.WorkingCurrency;
            var customer = shoppingCartItem.Item.Customer;
            var product = shoppingCartItem.Item.Product;
            var quantity = shoppingCartItem.Item.Quantity;

            if (product != null)
            {
                var attributesTotalPrice = new Money(currency);
                var pvaValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(shoppingCartItem.Item.AttributeSelection);

                foreach (var pvaValue in pvaValues)
                {
                    attributesTotalPrice += await GetProductVariantAttributeValuePriceAdjustmentAsync(pvaValue, product, customer, null, quantity);
                }

                var (discountAmount, appliedDiscount) = await GetDiscountAmountAsync(product, attributesTotalPrice, customer, quantity);

                return (currency.AsMoney(discountAmount.Amount * quantity), appliedDiscount);
            }

            return (new Money(currency), null);
        }

        #region Utilities

        private async Task<IEnumerable<TierPrice>> LoadTierPrices(Product product, Customer customer, PriceCalculationContext context = null)
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

        protected virtual async Task<ICollection<Discount>> GetAllowedDiscountsAsync(Product product, Customer customer, PriceCalculationContext context = null)
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

        protected virtual async Task<Money?> GetMinimumTierPriceAsync(Product product, Customer customer, int quantity, PriceCalculationContext context = null)
        {
            var tierPrices = await LoadTierPrices(product, customer, context);
            if (!tierPrices.Any())
            {
                return new Money(_workContext.WorkingCurrency);
            }

            var previousQty = 1;
            decimal? previousPrice = null;

            foreach (var tierPrice in tierPrices)
            {
                if (quantity < tierPrice.Quantity || tierPrice.Quantity < previousQty)
                {
                    continue;
                }

                if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Fixed)
                {
                    previousPrice = tierPrice.Price;
                }
                else if (tierPrice.CalculationMethod == TierPriceCalculationMethod.Percental)
                {
                    previousPrice = product.Price - (product.Price / 100 * tierPrice.Price);
                }
                else
                {
                    previousPrice = product.Price - tierPrice.Price;
                }

                previousQty = tierPrice.Quantity;
            }

            return previousPrice.HasValue ? new Money(previousPrice.Value, _workContext.WorkingCurrency) : null;
        }

        protected virtual async Task<Money> GetTierPriceAttributeAdjustmentAsync(Product product, Customer customer, int quantity, PriceCalculationContext context = null, decimal adjustment = 0)
        {
            var previousPrice = decimal.Zero;
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
                        previousPrice = adjustment - (adjustment / 100 * tierPrice.Price);
                    }

                    previousQty = tierPrice.Quantity;
                }
            }

            return new Money(previousPrice, _workContext.WorkingCurrency);
        }

        protected virtual async Task<Money> GetPreselectedPriceAsync(
            Product product,
            Customer customer,
            Currency currency,
            PriceCalculationContext context,
            ProductBundleItemData bundleItem,
            IEnumerable<ProductBundleItemData> bundleItems)
        {
            var attributesTotalPriceBase = new Money(currency);
            var preSelectedPriceAdjustmentBase = new Money(currency);
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
                        var attributeValuePriceAdjustment = await GetProductVariantAttributeValuePriceAdjustmentAsync(pvaValue, product, customer, context, 1);
                        var (priceAdjustmentBase, _) = await _taxService.GetProductPriceAsync(product, attributeValuePriceAdjustment, customer: customer);

                        preSelectedPriceAdjustmentBase += priceAdjustmentBase;
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
                        attributesTotalPriceBase += await GetProductVariantAttributeValuePriceAdjustmentAsync(value, product, customer, context, 1);
                    }
                }
                else
                {
                    attributesTotalPriceBase = preSelectedPriceAdjustmentBase;
                }
            }

            if (bundleItem != null)
            {
                bundleItem.AdditionalCharge = attributesTotalPriceBase;
            }

            var result = await GetFinalPriceAsync(product, bundleItems, attributesTotalPriceBase, customer, true, 1, bundleItem, context);
            return result;
        }

        #endregion
    }
}
