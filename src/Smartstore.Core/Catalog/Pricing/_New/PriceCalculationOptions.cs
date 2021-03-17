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
        public bool? GrossPrices { get; set; }
        public Currency TargetCurrency { get; set; }
        public Language Language { get; set; }
        public CashRoundingOptions CashRounding { get; set; } = new();

        public bool IgnoreDiscounts { get; set; }
        public bool IgnoreTierPrices { get; set; }
        public bool IgnoreAttributes { get; set; }
        public bool CalculateTax { get; set; } = true;
        public string TaxFormat { get; set; }

        public bool DetermineSelectionPrice { get; set; }
        public bool DetermineLowestPrice { get; set; }
        public bool DetermineMinTierPrice { get; set; }
        public bool DetermineMinAttributeCombinationPrice { get; set; }

        //public bool ApplyCartRules { get; set; }
    }
}
