using System.Threading.Tasks;
using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Core.Tests.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default)]
    public class CustomAdditionalChargeCalculator : IPriceCalculator
    {
        public static string AdditionalChargeKey => "CustomAdditionalCharge";

        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            var rawCustomCharge = context.Metadata.Get(AdditionalChargeKey);
            if (rawCustomCharge != null)
            {
                var customCharge = (decimal)rawCustomCharge;

                context.FinalPrice += customCharge;
                context.AdditionalCharge += customCharge;
            }

            await next(context);
        }
    }
}
