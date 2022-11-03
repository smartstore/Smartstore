using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public class ProductSummaryPriceMapper : CalculatedPriceMapper<SummaryPriceModel>
    {
        public ProductSummaryPriceMapper(IPriceLabelService labelService, PriceSettings priceSettings)
            : base(labelService, priceSettings)
        {
        }

        protected override Task MapCoreAsync(CalculatedPrice source, SummaryPriceModel model, dynamic parameters = null)
        {
            return Task.FromResult(model);
        }

        protected override bool ShouldMapRetailPrice(CalculatedPrice price)
        {
            // Never show retail price in listings if we have a regular price already
            return !price.RegularPrice.HasValue;
        }

        protected override ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel)
        {
            // TODO: (mc) (pricing) Check if label should be displayed in listings?
            return new ComparePriceModel
            {
                Price = comparePrice,
                Label = priceLabel.GetLocalized(x => x.ShortName)
            };
        }

        protected override void AddPromoBadges(CalculatedPrice price, SummaryPriceModel model)
        {
            if (_priceSettings.ShowDiscountSign && price.Saving.HasSaving)
            {
                model.Badges.Add(new PriceBadgeModel
                {
                    Label = T("Products.SavingBadgeLabel", price.Saving.SavingPercent.ToString("N0")),
                    Style = "danger"
                });
            }
            
            if (_priceSettings.ShowOfferBadgeInLists)
            {
                base.AddPromoBadges(price, model);
            }
        }
    }
}
