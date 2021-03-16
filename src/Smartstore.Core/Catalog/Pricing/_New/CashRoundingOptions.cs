using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Catalog.Pricing
{
    public class CashRoundingOptions
    {
        public bool RoundNetPrices { get; set; }
        public int TaxRoundingMethod { get; set; }
    }
}
