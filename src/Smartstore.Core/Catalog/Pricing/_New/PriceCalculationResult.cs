using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationResult
    {
        public IReadOnlyCollection<object> RequestSnapshots { get; init; }

        public decimal Price { get; init; }
    }
}