using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Catalog.")]
    public class PriceSettingsModel : ModelBase
    {
        [LocalizedDisplay("*ShowBasePriceInProductLists")]
        public bool ShowBasePriceInProductLists { get; set; }

        [LocalizedDisplay("*ShowVariantCombinationPriceAdjustment")]
        public bool ShowVariantCombinationPriceAdjustment { get; set; }

        [LocalizedDisplay("*ShowLoginForPriceNote")]
        public bool ShowLoginForPriceNote { get; set; }

        [LocalizedDisplay("*BundleItemShowBasePrice")]
        public bool BundleItemShowBasePrice { get; set; }

        [LocalizedDisplay("*ShowDiscountSign")]
        public bool ShowDiscountSign { get; set; }

        [LocalizedDisplay("*PriceDisplayStyle")]
        public PriceDisplayStyle PriceDisplayStyle { get; set; }

        [LocalizedDisplay("*PriceDisplayType")]
        public PriceDisplayType PriceDisplayType { get; set; }

        [LocalizedDisplay("*DisplayTextForZeroPrices")]
        public bool DisplayTextForZeroPrices { get; set; }

        [LocalizedDisplay("*IgnoreDiscounts")]
        public bool IgnoreDiscounts { get; set; }

        [LocalizedDisplay("*ApplyPercentageDiscountOnTierPrice")]
        public bool ApplyPercentageDiscountOnTierPrice { get; set; }

        [LocalizedDisplay("*ApplyTierPricePercentageToAttributePriceAdjustments")]
        public bool ApplyTierPricePercentageToAttributePriceAdjustments { get; set; }

        // NEW

        [LocalizedDisplay("*DefaultComparePriceLabelId")]
        public int? DefaultComparePriceLabelId { get; set; }

        [LocalizedDisplay("*DefaultRegularPriceLabelId")]
        public int? DefaultRegularPriceLabelId { get; set; }

        [LocalizedDisplay("*OfferPriceReplacesRegularPrice")]
        public bool OfferPriceReplacesRegularPrice { get; set; }

        [LocalizedDisplay("*AlwaysDisplayRetailPrice")]
        public bool AlwaysDisplayRetailPrice { get; set; } = true;

        [LocalizedDisplay("*ShowOfferCountdownRemainingHours")]
        public int? ShowOfferCountdownRemainingHours { get; set; } = 72;

        [LocalizedDisplay("*ShowOfferBadge")]
        public bool ShowOfferBadge { get; set; } = true;

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }

        [LocalizedDisplay("*OfferBadgeStyle")]
        public int OfferBadgeStyle { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeLabel")]
        public string LimitedOfferBadgeLabel { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeStyle")]
        public int LimitedOfferBadgeStyle { get; set; }
    }
}