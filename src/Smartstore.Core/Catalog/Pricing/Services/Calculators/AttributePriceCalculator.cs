using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing.Calculators
{
    /// <summary>
    /// TODO: (mg) (core) Describe
    /// </summary>
    [CalculatorUsage(CalculatorTargets.Product, CalculatorOrdering.Default + 10)]
    public class AttributePriceCalculator : IPriceCalculator
    {
        public async Task CalculateAsync(CalculatorContext context, CalculatorDelegate next)
        {
            // TODO ...
            await next(context);
        }
    }
}
