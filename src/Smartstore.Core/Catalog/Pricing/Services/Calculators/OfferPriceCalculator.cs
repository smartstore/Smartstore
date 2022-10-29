namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the offer price (aka special price), if any specified for the product.
    /// The offer price of bundle items is also taken into account if per-item pricing is activated for the bundle.
    /// We do not restrict and apply the offer price even if it is greater than the regular price,
    /// although this scenario is likely to be unusual in real life.
    /// </summary>
    [ServiceLifetime(ServiceLifetime.Singleton)]
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Default)]
    public class OfferPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            if (context.Options.IgnoreOfferPrice)
            {
                // Ignore offer price for this calculation.
                await next(context);
                return;
            }

            var product = context.Product;
            if (product.SpecialPrice.HasValue)
            {
                // Check date range.
                var now = DateTime.UtcNow;
                var from = product.SpecialPriceStartDateTimeUtc;
                var to = product.SpecialPriceEndDateTimeUtc;

                if ((from == null || now >= from) && (to == null || now <= to))
                {
                    context.OfferPrice = product.SpecialPrice;
                    context.OfferEndDateUtc = to;
                    context.FinalPrice = product.SpecialPrice.Value;
                }
            }

            await next(context);
        }
    }
}
