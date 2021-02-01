using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
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
using Smartstore.Core.Localization;
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
        private readonly CatalogSettings _catalogSettings;
        private readonly TaxSettings _taxSettings;

        public PriceCalculationService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            CatalogSettings catalogSettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _catalogSettings = catalogSettings;
            _taxSettings = taxSettings;
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
                    result = context.TierPrices
                        .GetOrLoad(product.Id)
                        .RemoveDuplicatedQuantities();
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
                    appliedDiscounts = context.AppliedDiscounts.GetOrLoad(product.Id);
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
                    productCategories = context.ProductCategories.GetOrLoad(product.Id);
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
                    productManufacturers = context.ProductManufacturers.GetOrLoad(product.Id);
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
            // TODO: (mg) (core) Complete PriceCalculationService (internal stuff required).
            var attributesTotalPriceBase = decimal.Zero;
            var preSelectedPriceAdjustmentBase = decimal.Zero;
            var isBundle = product.ProductType == ProductType.BundledProduct;
            var isBundleItemPricing = bundleItem?.Item?.BundleProduct?.BundlePerItemPricing ?? false;
            var isBundlePricing = bundleItem != null && !bundleItem.Item.BundleProduct.BundlePerItemPricing;
            var bundleItemId = bundleItem?.Item?.Id ?? 0;

            var query = new ProductVariantQuery();
            var selectedAttributeValues = new List<ProductVariantAttributeValue>();
            var attributes = context.Attributes.GetOrLoad(product.Id);

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
                //var attributeXml = query.CreateSelectedAttributesXml(product.Id, bundleItemId, attributes, _productAttributeParser, _services.Localization,
                //    _downloadService, _catalogSettings, _httpRequestBase, new List<string>());
                var selection = new ProductVariantAttributeSelection("");

                var combinations = context.AttributeCombinations.GetOrLoad(product.Id);

                // TODO: (mg) (core) Check whether equality check really works here (should only compare ProductVariantAttribute nodes!).
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

            //var result = GetFinalPrice(product, bundleItems, customer, attributesTotalPriceBase, true, 1, bundleItem, context);
            var result = decimal.Zero;
            return result;
        }

        #endregion
    }
}
