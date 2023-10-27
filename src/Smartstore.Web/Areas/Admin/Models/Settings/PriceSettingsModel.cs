using System.ComponentModel.DataAnnotations;
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

        [LocalizedDisplay("*ShowSavingBadgeInLists")]
        public bool ShowSavingBadgeInLists { get; set; }

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
        public bool AlwaysDisplayRetailPrice { get; set; }
        
        [LocalizedDisplay("*ShowOfferCountdownRemainingHours")]
        public int? ShowOfferCountdownRemainingHours { get; set; }

        [LocalizedDisplay("*ShowOfferBadge")]
        public bool ShowOfferBadge { get; set; }

        [LocalizedDisplay("*ShowOfferBadgeInLists")]
        public bool ShowOfferBadgeInLists { get; set; }

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }

        [LocalizedDisplay("*OfferBadgeStyle")]
        [UIHint("BadgeStyles")]
        public string OfferBadgeStyle { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeLabel")]
        public string LimitedOfferBadgeLabel { get; set; }

        [LocalizedDisplay("*LimitedOfferBadgeStyle")]
        [UIHint("BadgeStyles")]
        public string LimitedOfferBadgeStyle { get; set; }

        [LocalizedDisplay("*ShowPriceLabelInLists")]
        public bool ShowPriceLabelInLists { get; set; }

        [LocalizedDisplay("*ShowRetailPriceSaving")]
        public bool ShowRetailPriceSaving { get; set; }

        [LocalizedDisplay("*ValidateDiscountLimitationsInLists")]
        public bool ValidateDiscountLimitationsInLists { get; set; }

        [LocalizedDisplay("*ValidateDiscountRulesInLists")]
        public bool ValidateDiscountRulesInLists { get; set; }

        [LocalizedDisplay("*ValidateDiscountGiftCardsInLists")]
        public bool ValidateDiscountGiftCardsInLists { get; set; }

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