using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Price.")]
    public class PriceSettingsModel : ModelBase
    {
        #region Moved from CatalogSettings

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

        #endregion

        #region New in V5.0.1

        [LocalizedDisplay("*DefaultComparePriceLabelId")]
        public int? DefaultComparePriceLabelId { get; set; }

        [LocalizedDisplay("*DefaultRegularPriceLabelId")]
        public int? DefaultRegularPriceLabelId { get; set; }

        [LocalizedDisplay("*OfferPriceReplacesRegularPrice")]
        public bool OfferPriceReplacesRegularPrice { get; set; }

        [LocalizedDisplay("*AlwaysDisplayRetailPrice")]
        public bool AlwaysDisplayRetailPrice { get; set; } = true;
        
        // TODO: (mh) (pricing) Validation missing: must be positive if not null. Also applies to DiscountModel.
        [LocalizedDisplay("*ShowOfferCountdownRemainingHours")]
        public int? ShowOfferCountdownRemainingHours { get; set; } = 72;

        [LocalizedDisplay("*ShowOfferBadge")]
        public bool ShowOfferBadge { get; set; } = true;

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }

        [LocalizedDisplay("*OfferBadgeStyle")]
        public string OfferBadgeStyle { get; set; } = "dark";

        [LocalizedDisplay("*LimitedOfferBadgeLabel")]
        public string LimitedOfferBadgeLabel { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeStyle")]
        public string LimitedOfferBadgeStyle { get; set; } = "dark";

        #endregion
    }

    [LocalizedDisplay("Admin.Configuration.Settings.Price.")]
    public class PriceSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeLabel")]
        public string LimitedOfferBadgeLabel { get; set; }
    }
}