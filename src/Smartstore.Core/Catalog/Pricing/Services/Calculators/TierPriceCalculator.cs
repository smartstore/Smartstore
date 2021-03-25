using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) Describe
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO: (core) CatalogSettings.DisplayTierPricesWithDiscounts

            var product = context.Product;
            var options = context.Options;
            
            if (!options.IgnoreTierPrices && !options.IgnoreDiscounts && product.HasTierPrices)
            {
                var tierPrices = await LoadTierPrices(product, options.BatchContext);

                // TODO: (core) Really check IgnoreDiscounts here?
                var tierPrice = GetMinimumTierPrice(product, options.Customer, tierPrices, context.Quantity);
                if (tierPrice.HasValue)
                {
                    // TODO ...
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

            if (!batchContext.TierPrices.FullyLoaded)
            {
                await batchContext.TierPrices.LoadAllAsync();
            }

            var tierPrices = await batchContext.TierPrices.GetOrLoadAsync(product.Id);
            return tierPrices.RemoveDuplicatedQuantities();
        }
    }
}
