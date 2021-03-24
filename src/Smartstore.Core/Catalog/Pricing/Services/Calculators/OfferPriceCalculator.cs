using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) Describe
    /// </summary>
    [ServiceLifetime(ServiceLifetime.Singleton)]
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.Bundle, CalculatorOrdering.Default)]
    public class OfferPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var product = context.Product;
            if (product.SpecialPrice.HasValue)
            {
                // TODO: (mg) Does Bundle with ItemPricing has OfferPrice?
                // Check date range
                var now = DateTime.UtcNow;
                var from = product.SpecialPriceStartDateTimeUtc;
                var to = product.SpecialPriceEndDateTimeUtc;

                if ((from == null || now >= from) && (to == null || now <= to))
                {
                    // TODO: (mg) (core) Does it make sense here to set FinalPrice only when SpecialPrice is lower?
                    context.OfferPrice = product.SpecialPrice;
                    context.FinalPrice = product.SpecialPrice.Value;
                }
            }
            
            await next(context);
        }
    }
}
