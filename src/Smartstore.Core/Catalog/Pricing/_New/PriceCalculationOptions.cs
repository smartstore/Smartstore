using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceCalculationOptions
    {
        public bool? TaxInclusive { get; set; }
        public Currency TargetCurrency { get; set; }
        public Language Language { get; init; }
        public CashRoundingOptions CashRounding { get; set; }

        public bool IgnoreDiscounts { get; set; }
        public bool IgnoreTierPrices { get; set; }
        public bool IgnoreAttributes { get; set; }
        public bool CalculateTax { get; set; }
        public bool ApplyTaxFormat { get; set; }

        public bool DetermineSelectionPrice { get; set; }
        public bool DetermineLowestPrice { get; set; }
        public bool DetermineMinTierPrice { get; set; }
        public bool DetermineMinAttributeCombinationPrice { get; set; }

        //public bool ApplyCartRules { get; set; }
    }
}
