using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public class PriceModel : ModelBase
    {
        public Money FinalPrice { get; set; }
        public ComparePriceModel RegularPrice { get; set; }
        public ComparePriceModel RetailPrice { get; set; }
        public Money? ShippingSurcharge { get; set; }

        public DateTime? ValidUntilUtc { get; set; }
        public PriceSaving Saving { get; set; }

        public bool IsBasePriceEnabled { get; set; }
        public string BasePriceInfo { get; set; }

        public bool HasCalculation { get; set; }
        public bool CallForPrice { get; set; }
        public bool CustomerEntersPrice { get; set; }
        public bool ShowRetailPriceSaving { get; set; }

        public List<ProductBadgeModel> Badges { get; } = [];

        public bool HasDiscount
        {
            get => Saving.HasSaving;
        }
    }

    public class ProductSummaryPriceModel : PriceModel
    {
        public bool ShowSavingBadge { get; set; }
        public bool ShowPriceLabel { get; set; }
        public bool DisableBuyButton { get; set; }
        public bool DisableWishlistButton { get; set; }
        public bool AvailableForPreOrder { get; set; }
    }

    public class ProductDetailsPriceModel : PriceModel
    {
        public LocalizedString CountdownText { get; set; }
        public bool HidePrices { get; set; }
        public bool ShowLoginNote { get; set; }
        public bool BundleItemShowBasePrice { get; set; }

        public List<TierPriceModel> TierPrices { get; set; } = [];
    }

    public class ComparePriceModel
    {
        public Money Price { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class ProductBadgeModel
    {
        public string Label { get; set; }
        public string Style { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TierPriceModel
    {
        public int Quantity { get; set; }
        public Money Price { get; set; }
    }
}
