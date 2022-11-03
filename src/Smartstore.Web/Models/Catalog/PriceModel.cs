using Smartstore.Core.Catalog.Pricing;

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

        public bool CallForPrice { get; set; }
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

        public string CountdownText { get; set; }
        public bool CustomerEntersPrice { get; set; }
        public bool HidePrices { get; set; }
        public bool ShowLoginNote { get; set; }
        public bool BundleItemShowBasePrice { get; set; }
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
}
