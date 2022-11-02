namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// Calculates the regular price of a <see cref="Product"/>, in the primary currency,
    /// usually <see cref="Product.Price"/>, <see cref="Product.ComparePrice"/> or <see cref="Product.SpecialPrice"/>.
    /// </summary>
    [ServiceLifetime(ServiceLifetime.Singleton)]
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Late + 1)]
    public class RegularPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;

            if (context.DiscountAmount > 0)
            {
                if (context.OfferPrice.HasValue)
                {
                    if (product.ComparePrice > 0)
                    {
                        context.RegularPrice = Math.Min(context.OfferPrice.Value, product.ComparePrice);
                    }
                    else
                    {
                        context.RegularPrice = Math.Min(context.OfferPrice.Value, product.Price);
                    }
                }
                else
                {
                    if (product.ComparePrice > 0)
                    {
                        context.RegularPrice = Math.Min(product.Price, product.ComparePrice);
                    }
                    else
                    {
                        context.RegularPrice = product.Price;
                    }
                }
            }

            if (context.OfferPrice.HasValue)
            {
                if (product.ComparePrice > 0)
                {
                    // PAngV: "Price" would not be allowed if greater than "ComparePrice".
                    context.RegularPrice = Math.Min(product.Price, product.ComparePrice);
                }
                else
                {
                    context.RegularPrice = product.Price;
                }
            }

            if (product.ComparePrice > product.Price)
            {
                context.RegularPrice = product.ComparePrice;
            }

            await next(context);
        }
    }
}
