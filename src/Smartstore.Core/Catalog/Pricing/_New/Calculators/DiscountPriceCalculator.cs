using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.All, CalculatorOrdering.Late)]
    public class DiscountPriceCalculator : IPriceCalculator
    {
        private readonly SmartDbContext _db;
        private readonly IDiscountService _discountService;

        public DiscountPriceCalculator(SmartDbContext db, IDiscountService discountService)
        {
            _db = db;
            _discountService = discountService;
        }

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            var bundleItem = context.BundleItem;

            if (bundleItem?.Item != null)
            {
                var bi = bundleItem.Item;
                if (bi.Discount.HasValue && bi.BundleProduct.BundlePerItemPricing)
                {
                    var appliedDiscount = new Discount
                    {
                        UsePercentage = bi.DiscountPercentage,
                        DiscountPercentage = bi.Discount.Value,
                        DiscountAmount = bi.Discount.Value
                    };

                    context.AppliedDiscounts.Add(appliedDiscount);
                    var discountAmount = appliedDiscount.GetDiscountAmount(context.FinalPrice);
                    context.FinalPrice -= discountAmount;
                }
            }
            else if (!context.Options.IgnoreDiscounts && !product.CustomerEntersPrice)
            {
                // Don't calculate when customer entered price or discounts should be ignored in any case.
                var applicableDiscounts = await GetApplicableDiscounts(product, context);
                if (applicableDiscounts.Any())
                {
                    var preferredDiscount = applicableDiscounts.GetPreferredDiscount(context.FinalPrice);

                    if (preferredDiscount != null)
                    {
                        context.AppliedDiscounts.Add(preferredDiscount);
                        var discountAmount = preferredDiscount.GetDiscountAmount(context.FinalPrice);
                        context.FinalPrice -= discountAmount;
                    }
                }
            }

            await next(context);
        }

        protected virtual async Task<ICollection<Discount>> GetApplicableDiscounts(Product product, CalculatorContext context)
        {
            var result = new HashSet<Discount>();
            var batchContext = context.Options.BatchContext;
            var customer = context.Options.Customer;

            // Check discounts assigned to the product.
            // We use the property "HasDiscountsApplied" for performance optimization to avoid unnecessary database calls.
            if (product.HasDiscountsApplied)
            {
                var collectionLoaded = _db.IsCollectionLoaded(product, x => x.AppliedDiscounts);
                if (!collectionLoaded)
                {
                    if (!batchContext.AppliedDiscounts.FullyLoaded)
                    {
                        await batchContext.AppliedDiscounts.LoadAllAsync();
                    }
                }

                var appliedDiscounts = collectionLoaded ? product.AppliedDiscounts : await batchContext.AppliedDiscounts.GetOrLoadAsync(product.Id);

                if (appliedDiscounts != null)
                {
                    await TryAddDiscounts(appliedDiscounts, result, DiscountType.AssignedToSkus, context.Options);
                }
            }

            // Check discounts assigned to categories.
            var categoryDiscounts = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToCategories);
            if (categoryDiscounts.Any())
            {
                if (!batchContext.ProductCategories.FullyLoaded)
                {
                    await batchContext.ProductCategories.LoadAllAsync();
                }

                var productCategories = await batchContext.ProductCategories.GetOrLoadAsync(product.Id);

                foreach (var productCategory in productCategories)
                {
                    var category = productCategory.Category;

                    if (category == null)
                        continue;

                    if (category.HasDiscountsApplied)
                    {
                        //// INFO: AppliedDiscounts are eager loaded already.
                        //await _db.LoadCollectionAsync(category, x => x.AppliedDiscounts);

                        await TryAddDiscounts(category.AppliedDiscounts, result, DiscountType.AssignedToCategories, context.Options);
                    }
                }
            }

            // Check discounts assigned to manufacturers.
            var manufacturerDiscounts = await _discountService.GetAllDiscountsAsync(DiscountType.AssignedToCategories);
            if (manufacturerDiscounts.Any())
            {
                if (!batchContext.ProductManufacturers.FullyLoaded)
                {
                    await batchContext.ProductManufacturers.LoadAllAsync();
                }

                var productManufacturers = await batchContext.ProductManufacturers.GetOrLoadAsync(product.Id);

                foreach (var productManufacturer in productManufacturers)
                {
                    var manu = productManufacturer.Manufacturer;

                    if (manu == null)
                        continue;

                    if (manu.HasDiscountsApplied)
                    {
                        //// INFO: AppliedDiscounts are eager loaded already.
                        //await _db.LoadCollectionAsync(manu, x => x.AppliedDiscounts);

                        await TryAddDiscounts(manu.AppliedDiscounts, result, DiscountType.AssignedToManufacturers, context.Options);
                    }
                }
            }

            return result;
        }

        private async Task TryAddDiscounts(ICollection<Discount> source, HashSet<Discount> target, DiscountType type, PriceCalculationOptions options)
        {
            foreach (var discount in source)
            {
                // TODO: (mg) (core) Pass through context data from CalculationOptions (Store etc.)
                if (discount.DiscountType == type && !target.Contains(discount) && await _discountService.IsDiscountValidAsync(discount, options.Customer))
                {
                    bool isValid;
                    if (options.CheckDiscountValidity)
                    {
                        isValid = await _discountService.IsDiscountValidAsync(discount, options.Customer);
                    }
                    else
                    {
                        // If validity check is disabled: exclude discounts which require coupon codes or have any usage restriction.
                        isValid = !discount.RequiresCouponCode && discount.DiscountLimitation == DiscountLimitationType.Unlimited;
                    }

                    if (isValid)
                    {
                        target.Add(discount);
                    }
                }
            }
        }
    }
}
