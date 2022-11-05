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

        /// <summary>
        /// Gets or sets a value indicating whether to show savings badge in product lists
        /// </summary>
        public bool ShowSavingBadgeInLists { get; set; } = true;

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
        /// The system default id of PriceLabel entity for product compare prices.
        /// Takes effect when a product does not override the ComparePrice label.
        /// </summary>
        public int? DefaultComparePriceLabelId { get; set; }

        /// <summary>
        /// The system default id of PriceLabel entity to use for the crossed out regular price.
        /// Takes effect when there is an offer or a discount has been applied to a product.
        /// </summary>
        public int? DefaultRegularPriceLabelId { get; set; }

        /// <summary>
        /// If TRUE, the special offer price just replaces the regular price
        /// as if there was no offer. If FALSE, the regular price will be displayed crossed out.
        /// </summary>
        public bool OfferPriceReplacesRegularPrice { get; set; }

        /// <summary>
        /// If TRUE, the MSRP will be displayed in product detail even if there is already an offer or a discount.
        /// In this case the MSRP will appear as another crossed out price alongside the discounted price.
        /// </summary>
        public bool AlwaysDisplayRetailPrice { get; set; } = true;

        /// <summary>
        /// Sets the offer remaining time (in hours) from which a countdown should be displayed in product detail,
        /// e.g. "ends in 3 hours, 23 min.". To hide the countdown, set this to NULL.
        /// Only applies to limited time offers with a non-null end date.
        /// </summary>
        public int? ShowOfferCountdownRemainingHours { get; set; } = 72;

        /// <summary>
        /// If TRUE, displays a badge if an offer price is active.
        /// </summary>
        public bool ShowOfferBadge { get; set; } = true;

        /// <summary>
        /// If TRUE, displays a badge in product lists if an offer price is active.
        /// <see cref="ShowOfferBadge"/> must be TRUE for this to take effect.
        /// </summary>  
        public bool ShowOfferBadgeInLists { get; set; } = true;

        /// <summary>
        /// The label of the offer badge, e.g. "Deal".
        /// </summary>
        [LocalizedProperty]
        public string OfferBadgeLabel { get; set; }

        /// <summary>
        /// The style of the offer badge.
        /// </summary>
        public string OfferBadgeStyle { get; set; } = "dark";

        /// <summary>
        /// The label of the offer badge if the offer is limited, e.g. "Limited time deal".
        /// </summary>
        [LocalizedProperty]
        public string LimitedOfferBadgeLabel { get; set; }

        /// <summary>
        /// The style of the limited time offer badge.
        /// </summary>
        public string LimitedOfferBadgeStyle { get; set; } = "dark";

        /// <summary>
        /// If TRUE, displays the compare price label's short name in product lists.
        /// </summary>
        public bool ShowPriceLabelInLists { get; set; } = true;

        /// <summary>
        /// If TRUE, displays price saving even if the reference price is the retail price only.
        /// </summary>
        public bool ShowRetailPriceSaving { get; set; } = true;

        #endregion
    }
}
