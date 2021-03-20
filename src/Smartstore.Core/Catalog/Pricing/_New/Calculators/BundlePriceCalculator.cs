using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    [CalculatorUsage(CalculatorTargets.Bundle, CalculatorOrdering.Early)]
    public class BundlePriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO ...
            await next(context);
        }
    }
}
