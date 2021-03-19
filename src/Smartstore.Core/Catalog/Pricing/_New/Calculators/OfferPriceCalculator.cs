using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [ServiceLifetime(ServiceLifetime.Singleton)]
    [CalculatorUsage(CalculatorTargets.Product | CalculatorTargets.CartItem, CalculatorOrdering.Default)]
    public class OfferPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            if (context.Product is Product product && product.SpecialPrice.HasValue && product.ProductType != ProductType.GroupedProduct)
            {
                // Check date range
                var now = DateTime.UtcNow;
                var from = product.SpecialPriceStartDateTimeUtc;
                var to = product.SpecialPriceEndDateTimeUtc;

                if ((from == null || now >= from) && (to == null || now <= to))
                {
                    context.OfferPrice = product.SpecialPrice;
                    context.FinalPrice = product.SpecialPrice.Value;
                }
            }
            
            await next(context);
        }
    }
}
