using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    public class OfferPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            if (context.Product is Product product)
            {
                if (product.SpecialPrice.HasValue)
                {
                    // Check date range
                    DateTime now = DateTime.UtcNow;
                    if (product.SpecialPriceStartDateTimeUtc.HasValue)
                    {
                        DateTime startDate = DateTime.SpecifyKind(product.SpecialPriceStartDateTimeUtc.Value, DateTimeKind.Utc);
                        if (startDate.CompareTo(now) > 0)
                        {
                            await next(context);
                            return;
                        }
                    }

                    if (product.SpecialPriceEndDateTimeUtc.HasValue)
                    {
                        DateTime endDate = DateTime.SpecifyKind(product.SpecialPriceEndDateTimeUtc.Value, DateTimeKind.Utc);
                        if (endDate.CompareTo(now) < 0)
                        {
                            await next(context);
                            return;
                        }
                    }

                    context.OfferPrice = product.SpecialPrice;
                    context.SetFinalPrice(product.SpecialPrice.Value);
                }
            }
            
            await next(context);
        }
    }
}
