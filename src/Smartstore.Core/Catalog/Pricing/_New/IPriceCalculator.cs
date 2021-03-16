using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public delegate Task CalculatorDelegate(CalculatorContext context);

    public interface IPriceCalculator
    {
        Task CalculateAsync(CalculatorContext context, CalculatorDelegate next);
    }
}
