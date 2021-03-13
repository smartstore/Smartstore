using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationOptions
    {
        public bool SourcePricesIncludeTax { get; set; }
        public bool IgnoreDiscounts { get; set; }
        public bool IgnoreTierPrices { get; set; }
        public bool IgnoreAttributes { get; set; }
        public bool CalculateTax { get; set; }
        public bool ApplyTaxFormat { get; set; }
        public bool DeterminePreselectedPrice { get; set; }
        public bool DetermineLowestPrice { get; set; }
        //public bool ApplyCartRules { get; set; }
        public Currency TargetCurrency { get; set; }
    }
}
