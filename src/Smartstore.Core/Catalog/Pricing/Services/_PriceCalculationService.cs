using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Customers;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Stores;

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
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly CatalogSettings _catalogSettings;
        private readonly TaxSettings _taxSettings;

        public PriceCalculationService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            IProductAttributeMaterializer productAttributeMaterializer,
            CatalogSettings catalogSettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _catalogSettings = catalogSettings;
            _taxSettings = taxSettings;
        }

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

        public virtual decimal? GetSpecialPrice(Product product)
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

        public virtual async Task<decimal> GetProductCostAsync(Product product, ProductVariantAttributeSelection selection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(selection, nameof(selection));

            var result = product.ProductCost;
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
                    if (linkedProductsDic.TryGetValue(value.LinkedProductId, out var productCost))
                    {
                        result += productCost * value.Quantity;
                    }
                }
            }

            return result;
        }

        public virtual async Task<decimal> GetPreselectedPriceAsync(Product product, Customer customer, Currency currency, PriceCalculationContext context)
        {
            Guard.NotNull(product, nameof(product));

            var result = decimal.Zero;

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

                result = await GetPreselectedPriceAsync(product, customer, currency, context, null, bundleItemsData);
            }
            else
            {
                result = await GetPreselectedPriceAsync(product, customer, currency, context, null, null);
            }

            return result;
        }

        public virtual async Task<(decimal Amount, Discount AppliedDiscount)> GetDiscountAmountAsync(
            Product product,
            Customer customer = null,
            decimal additionalCharge = decimal.Zero,
            int quantity = 1,
            ProductBundleItemData bundleItem = null,
            PriceCalculationContext context = null,
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

                    // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
                    //var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAsync(product, customer, additionalCharge, false, quantity, bundleItem, context);
                    var finalPriceWithoutDiscount = decimal.Zero;
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

                // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
                //var finalPriceWithoutDiscount = finalPrice ?? await GetFinalPriceAsync(product, customer, additionalCharge, false, quantity, bundleItem, context);
                var finalPriceWithoutDiscount = decimal.Zero;
                appliedDiscount = allowedDiscounts.GetPreferredDiscount(finalPriceWithoutDiscount);

                if (appliedDiscount != null)
                {
                    discountAmount = appliedDiscount.GetDiscountAmount(finalPriceWithoutDiscount);
                }
            }

            return (discountAmount, appliedDiscount);
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
                        .Where(x => x.StoreId == 0 || x.StoreId == _storeContext.CurrentStore.Id)
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
            // TODO: (mg) (core) Complete PriceCalculationService (IDiscountService required).
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
                        //if (discount.DiscountType == DiscountType.AssignedToSkus && !result.Contains(discount) && _discountService.IsDiscountValid(discount, customer))
                        //{
                        //    result.Add(discount);
                        //}
                    }
                }
            }

            // Check discounts assigned to categories.
            //var discountsAssignedToCategories = _discountService.GetAllDiscounts(DiscountType.AssignedToCategories);
            var discountsAssignedToCategories = Enumerable.Empty<Discount>();
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
                                //if (discount.DiscountType == DiscountType.AssignedToCategories && !result.Contains(discount) && _discountService.IsDiscountValid(discount, customer))
                                //{
                                //    result.Add(discount);
                                //}
                            }
                        }
                    }
                }
            }

            // Check discounts assigned to manufacturers.
            //var discountsAssignedToManufacturers = _discountService.GetAllDiscounts(DiscountType.AssignedToManufacturers);
            var discountsAssignedToManufacturers = Enumerable.Empty<Discount>();
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
                                //if (discount.DiscountType == DiscountType.AssignedToManufacturers && !result.Contains(discount) && _discountService.IsDiscountValid(discount, customer))
                                //{
                                //    result.Add(discount);
                                //}
                            }
                        }
                    }
                }
            }

            return result;
        }

        protected virtual async Task<decimal?> GetMinimumTierPriceAsync(Product product, Customer customer, int quantity, PriceCalculationContext context = null)
        {
            var tierPrices = await LoadTierPrices(product, customer, context);
            if (!tierPrices.Any())
            {
                return decimal.Zero;
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

            return previousPrice;
        }

        protected virtual async Task<decimal> GetTierPriceAttributeAdjustmentAsync(Product product, Customer customer, int quantity, PriceCalculationContext context = null, decimal adjustment = 0)
        {
            var tierPrices = await LoadTierPrices(product, customer, context);
            if (!tierPrices.Any())
            {
                return decimal.Zero;
            }

            var previousQty = 1;
            var previousPrice = decimal.Zero;

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

            return previousPrice;
        }

        protected virtual async Task<decimal> GetPreselectedPriceAsync(
            Product product,
            Customer customer,
            Currency currency,
            PriceCalculationContext context,
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
                        // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
                        //var attributeValuePriceAdjustment = GetProductVariantAttributeValuePriceAdjustment(pvaValue, product, customer, context, 1);
                        var attributeValuePriceAdjustment = decimal.Zero;
                        var priceAdjustmentBase = await _taxService.GetProductPriceAsync(product, attributeValuePriceAdjustment, currency: currency, customer: customer );

                        preSelectedPriceAdjustmentBase = decimal.Add(preSelectedPriceAdjustmentBase, priceAdjustmentBase);
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
                    // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
                    //selectedAttributeValues.Each(x => attributesTotalPriceBase += GetProductVariantAttributeValuePriceAdjustment(x, product, customer, context, 1));
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

            // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
            //var result = GetFinalPrice(product, bundleItems, customer, attributesTotalPriceBase, true, 1, bundleItem, context);
            var result = decimal.Zero;
            return result;
        }

        #endregion
    }
}
