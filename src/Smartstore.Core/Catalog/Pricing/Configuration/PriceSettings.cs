using Smartstore.Core.Configuration;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceSettings : ISettings
    {
        #region Moved from CatalogSettings

        /// <summary>
        /// Gets or sets a value indicating whether to display the base price of a product
        /// </summary>
        public bool ShowBasePriceInProductLists { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display price adjustment of a product variant combination
        /// </summary>
        public bool ShowVariantCombinationPriceAdjustment { get; set; } = true;

        /// <summary>
        /// Indicates whether to show a login note if the user is not authorized to see prices.
        /// </summary>
        public bool ShowLoginForPriceNote { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether base price should be rendered for bundle items
        /// </summary>
        public bool BundleItemShowBasePrice { get; set; }

        public bool ShowDiscountSign { get; set; } = true;

        /// <summary>
        /// Gets or sets the price display style for prices
        /// </summary>
        public PriceDisplayStyle PriceDisplayStyle { get; set; }

        /// <summary>
        /// Gets or sets the price display type for prices in product lists
        /// </summary>
        public PriceDisplayType PriceDisplayType { get; set; }

        /// <summary>
        /// Displays a textual resources instead of the decimal value when prices are 0
        /// </summary>
        public bool DisplayTextForZeroPrices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to always ignore discounts.
        /// Discounts explicitly offered for bundle items are excluded from this. They are always applied.
        /// </summary>
        public bool IgnoreDiscounts { get; set; }

        /// <summary>
        /// Gets or sets whether to also apply percentage discounts in tier prices.
        /// </summary>
        public bool ApplyPercentageDiscountOnTierPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether percental values of tierprices should be applied to price adjustments of attributes
        /// </summary>
        public bool ApplyTierPricePercentageToAttributePriceAdjustments { get; set; } = false;

        #endregion

        #region New

        /// <summary>
        /// TODO: Describe (P)
        /// </summary>
        public int? DefaultComparePriceLabelId { get; set; }

        /// <summary>
        /// TODO: Describe
        /// </summary>
        public int? DefaultRegularPriceLabelId { get; set; }

        /// <summary>
        /// TODO: Describe (P)
        /// </summary>
        public bool SpecialPriceReplacesRegularPrice { get; set; }

        /// <summary>
        /// TODO: Describe
        /// </summary>
        public bool AlwaysDisplayRetailPrice { get; set; } = true;

        /// <summary>
        /// TODO: Describe
        /// </summary>
        public int? ShowSpecialPriceCountdownRemainingHours { get; set; }

        /// <summary>
        /// TODO: Describe
        /// </summary>
        [LocalizedProperty]
        public string SpecialPriceBadgeLabel { get; set; }

        /// <summary>
        /// TODO: Describe
        /// </summary>
        public int SpecialPriceBadgeStyle { get; set; }

        // TBD: ShowSaving, SavingDisplayStyle

        #endregion
    }
}
