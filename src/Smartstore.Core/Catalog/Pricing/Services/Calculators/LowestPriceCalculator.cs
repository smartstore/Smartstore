using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) The decription is too inaccurate. What does lowest possible price mean? Explain please.
    /// Calculates the lowest possible product price.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 10)]
    public class LowestPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            if (!context.Options.DetermineLowestPrice)
            {
                // Proceed with pipeline and omit this calculator, it is made for lowest price calculation only.
                await next(context);
                return;
            }

            // Process the whole pipeline with maximum quantity to get the minimum tier price applied.
            // TODO: (core) Shift Quantity from context to options. Options are shallow copied, context not. (MC)
            context.Quantity = int.MaxValue;
            await next(context);

            // Get lowest possible price.
            var product = context.Product;
            var lowestPrice = context.FinalPrice;

            if (product.LowestAttributeCombinationPrice.HasValue && product.LowestAttributeCombinationPrice.Value < lowestPrice)
            {
                lowestPrice = product.LowestAttributeCombinationPrice.Value;
            }

            if (lowestPrice == decimal.Zero && product.Price == decimal.Zero)
            {
                lowestPrice = product.LowestAttributeCombinationPrice ?? decimal.Zero;
            }

            if (lowestPrice < context.FinalPrice)
            {
                context.FinalPrice = lowestPrice;
            }

            context.LowestPrice = context.FinalPrice;

            // Check whether the product has a price range.
            if (!context.HasPriceRange)
            {
                context.HasPriceRange = product.LowestAttributeCombinationPrice.HasValue;
            }

            if (!context.HasPriceRange)
            {
                var attributes = await context.Options.BatchContext.Attributes.GetOrLoadAsync(product.Id);
                context.HasPriceRange = attributes.Any(x => x.ProductVariantAttributeValues.Any(y => y.PriceAdjustment != decimal.Zero));
            }
        }
    }
}
