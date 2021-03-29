using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the minimum tier price and applies it if it is lower than the FinalPrice.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            var options = context.Options;
            // Ignore tier prices of bundle items (BundlePerItemPricing).
            var processTierPrices = !options.IgnoreTierPrices && !options.IgnoreDiscounts && product.HasTierPrices && context.BundleItem?.Item == null;

            if (processTierPrices)
            {
                var tierPrices = await options.BatchContext.TierPrices.GetOrLoadAsync(product.Id);
                tierPrices = tierPrices.RemoveDuplicatedQuantities();

                // Put minimum tier price to context because it's required for discount calculation.
                context.MinTierPrice = GetMinimumTierPrice(product, tierPrices, context.Quantity);

                if (context.Options.DetermineLowestPrice && !context.HasPriceRange)
                {
                    context.HasPriceRange = tierPrices.Any() && !(tierPrices.Count == 1 && tierPrices.First().Quantity <= 1);
                }
            }

            // Process the whole pipeline. We need the result of discount calculation.
            await next(context);

            if (processTierPrices && context.MinTierPrice.HasValue)
            {
                // Apply the minimum tier price if it achieves a lower price than the discounted FinalPrice.
                context.FinalPrice = Math.Min(context.FinalPrice, context.MinTierPrice.Value);
            }
        }

        protected virtual decimal? GetMinimumTierPrice(Product product, IEnumerable<TierPrice> tierPrices, int quantity)
        {
            decimal? result = null;
            var previousQty = 1;

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
    }
}
