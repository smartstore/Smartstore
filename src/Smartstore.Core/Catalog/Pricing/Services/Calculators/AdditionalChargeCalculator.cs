using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    // TODO: (mg) (core) Check at the end whether AdditionalChargeCalculator is still needed at all.
    /// <summary>
    /// A simple calculator that just adds <see cref="PriceCalculationContext.AdditionalCharge"/> to <see cref="CalculatorContext.FinalPrice"/>.
    /// </summary>
    //[CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Late)]
    //public class AdditionalChargeCalculator : IPriceCalculator
    //{
    //    public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
    //    {
    //        await next(context);

    //        context.FinalPrice += context.AdditionalCharge;
    //    }
    //}
}
