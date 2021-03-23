using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 100)]
    public class TierPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO: (core) ICatalogSettings.DisplayTierPricesWithDiscounts

            var product = context.Product;
            var options = context.Options;
            
            if (!options.IgnoreTierPrices && !options.IgnoreDiscounts && product.HasTierPrices)
            {
                // TODO: (core) Really check IgnoreDiscounts here?
                var tierPrice = await GetMinimumTierPriceAsync(product, options.Customer, context.Quantity, options.BatchContext);
                if (tierPrice.HasValue)
                {
                    // TODO ...
                }
            }

            await next(context);
        }

        protected virtual async Task<decimal?> GetMinimumTierPriceAsync(Product product, Customer customer, int quantity, ProductBatchContext batchContext)
        {
            if (!product.HasTierPrices)
            {
                return decimal.Zero;
            }

            var previousQty = 1;
            decimal? result = null;
            var tierPrices = await LoadTierPrices(product, customer, batchContext);

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

        private static async Task<IEnumerable<TierPrice>> LoadTierPrices(Product product, Customer customer, ProductBatchContext batchContext)
        {
            if (!product.HasTierPrices)
                return Enumerable.Empty<TierPrice>();

            if (!batchContext.TierPrices.FullyLoaded)
            {
                await batchContext.TierPrices.LoadAllAsync();
            }

            var tierPrices = await batchContext.TierPrices.GetOrLoadAsync(product.Id);
            return tierPrices.RemoveDuplicatedQuantities();
        }
    }
}
