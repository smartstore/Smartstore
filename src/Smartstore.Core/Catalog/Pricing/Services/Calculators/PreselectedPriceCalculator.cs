using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) Describe
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 20)]
    public class PreselectedPriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO ...
            await next(context);
        }
    }
}
