using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog
{
    public class PriceModel : ModelBase
    {
        public PriceModel(CalculatedPrice price)
        {
            CalculatedPrice = Guard.NotNull(price, nameof(price));
        }

        public CalculatedPrice CalculatedPrice { get; }
        public List<PriceBadgeModel> Badges { get; } = new();

        public ComparePriceModel RegularPrice { get; set; }
        public ComparePriceModel RetailPrice { get; set; }

        public Money FinalPrice
        {
            get => CalculatedPrice.FinalPrice;
        }

        public DateTime? ValidUntilUtc
        {
            get => CalculatedPrice.ValidUntilUtc;
        }

        public PriceSaving Saving
        {
            get => CalculatedPrice.Saving;
        }

        public bool IsBasePriceEnabled { get; set; }
        public string BasePriceInfo { get; set; }

        public bool CallForPrice { get; set; }
        public bool ShowRetailPriceSaving { get; set; }
    }

    public class SummaryPriceModel : PriceModel
    {
        public SummaryPriceModel(CalculatedPrice price)
            : base(price)
        {
        }

        public bool DisableBuyButton { get; set; }
        public bool DisableWishlistButton { get; set; }
        public bool AvailableForPreOrder { get; set; }
    }

    public class DetailsPriceModel : PriceModel
    {
        public DetailsPriceModel(CalculatedPrice price)
            : base(price)
        {
        }

        public LocalizedString CountdownText { get; set; }
        public bool CustomerEntersPrice { get; set; }
        public bool HidePrices { get; set; }
        public bool ShowLoginNote { get; set; }
        public bool BundleItemShowBasePrice { get; set; }

        public List<TierPriceModel> TierPrices { get; set; } = new();
    }

    public class ComparePriceModel
    {
        public Money Price { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
    }

    public class PriceBadgeModel
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
