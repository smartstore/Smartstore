using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the lowest possible price a product can achieve. 
    /// The lowest tier price, the lowest attribute combination price and discounts are taken into account during calculation.
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Default + 10)]
    public class LowestPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;

            if (!context.Options.DetermineLowestPrice || (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing))
            {
                // Proceed with pipeline and omit this calculator, it is made for lowest price calculation only.
                await next(context);
                return;
            }

            // Process the whole pipeline with maximum quantity to get the minimum tier price applied.
            context.Quantity = int.MaxValue;
            await next(context);

            // Get lowest possible price.
            var lowestPrice = context.FinalPrice;
            var forceApply = false;

            if (product.LowestAttributeCombinationPrice.HasValue && product.LowestAttributeCombinationPrice.Value < lowestPrice)
            {
                lowestPrice = product.LowestAttributeCombinationPrice.Value;
            }

            if (lowestPrice == decimal.Zero && product.Price == decimal.Zero)
            {
                // Do not display 0 as lowest price.
                forceApply = true;
                lowestPrice = product.LowestAttributeCombinationPrice ?? decimal.Zero;
            }

            // Apply lowest price.
            if (lowestPrice < context.FinalPrice || forceApply)
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
