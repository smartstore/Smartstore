using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the minimum tier price and applies it if it's lower than the FinalPrice.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO: (core) CatalogSettings.DisplayTierPricesWithDiscounts is an old hidden setting that does absolutely nothing. What is the meaning of it?

            var product = context.Product;
            var options = context.Options;

            // Ignore tier prices of bundle items (BundlePerItemPricing).
            if (!options.IgnoreTierPrices && !options.IgnoreDiscounts && product.HasTierPrices && context.BundleItem?.Item == null)
            {
                var tierPrices = await LoadTierPrices(product, options.BatchContext);
                var tierPrice = GetMinimumTierPrice(product, options.Customer, tierPrices, context.Quantity);

                if (tierPrice.HasValue)
                {
                    // Keep the minimum tier price because it's required for discount calculation.
                    context.MinTierPrice = tierPrice.Value;

                    // Previously, the tier price was not applied here if a discount achieved a smaller FinalPrice.
                    if (tierPrice.Value < context.FinalPrice)
                    {
                        context.FinalPrice = tierPrice.Value;
                    }

                    //if (!options.IgnorePercentageDiscountOnTierPrices)
                    //{
                    //    context.FinalPrice -= GetPercentageDiscountAmount(context, product, tierPrice.Value);
                    //}
                }

                if (context.Options.DetermineLowestPrice && !context.HasPriceRange)
                {
                    context.HasPriceRange = tierPrices.Any() && !(tierPrices.Count() == 1 && tierPrices.First().Quantity <= 1);
                }
            }

            await next(context);
        }

        protected virtual decimal? GetMinimumTierPrice(Product product, Customer customer, IEnumerable<TierPrice> tierPrices, int quantity)
        {
            if (!product.HasTierPrices)
            {
                return decimal.Zero;
            }

            var previousQty = 1;
            decimal? result = null;

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

        private static async Task<IEnumerable<TierPrice>> LoadTierPrices(Product product, ProductBatchContext batchContext)
        {
            if (!product.HasTierPrices)
            {
                return Enumerable.Empty<TierPrice>();
            }

            //if (!batchContext.TierPrices.FullyLoaded)
            //{
            //    await batchContext.TierPrices.LoadAllAsync();
            //}

            var tierPrices = await batchContext.TierPrices.GetOrLoadAsync(product.Id);
            return tierPrices.RemoveDuplicatedQuantities();
        }

        //private decimal GetPercentageDiscountAmount(CalculatorContext context, Product product, decimal tierPrice)
        //{
        //    Discount discount = null;
        //    var bundleItem = context.BundleItem?.Item;

        //    if (bundleItem != null)
        //    {
        //        if (bundleItem.BundleProduct.BundlePerItemPricing &&
        //            bundleItem.Discount.HasValue &&
        //            bundleItem.DiscountPercentage)
        //        {
        //            discount = new Discount
        //            {
        //                UsePercentage = true,
        //                DiscountPercentage = bundleItem.Discount.Value,
        //                DiscountAmount = bundleItem.Discount.Value
        //            };
        //        }
        //    }
        //    else if (!product.CustomerEntersPrice)
        //    {
        //    }

        //    return discount?.GetDiscountAmount(tierPrice) ?? decimal.Zero;
        //}
    }
}
