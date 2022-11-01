using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Web.Models.Catalog
{
    public class UnitPriceModel : ModelBase
    {
        public UnitPriceModel(CalculatedPrice price)
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

        public PriceSaving Saving
        {
            get => CalculatedPrice.Saving;
        }     
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
