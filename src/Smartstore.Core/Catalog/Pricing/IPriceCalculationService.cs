using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public interface IPriceCalculationService
    {
        Task<PriceCalculationResult> Calculate(PriceCalculationRequest request, IEnumerable<IPriceCalculator> pipeline);
    }
}
