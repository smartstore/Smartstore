using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public class ProductSummaryPriceMapper : CalculatedPriceMapper<SummaryPriceModel>
    {
        public ProductSummaryPriceMapper(
            IPriceCalculationService priceCalculationService, 
            IPriceLabelService labelService, 
            PriceSettings priceSettings)
            : base(priceCalculationService, labelService, priceSettings)
        {
        }

        protected override Task<bool> MapCoreAsync(CalculatedPrice price, SummaryPriceModel model, dynamic parameters = null)
        {
            if (_priceSettings.ShowSavingBadgeInLists && price.Saving.HasSaving)
            {
                model.Badges.Add(new PriceBadgeModel
                {
                    Label = T("Products.SavingBadgeLabel", price.Saving.SavingPercent.ToString("N0")),
                    Style = "danger"
                });
            }

            if (_priceSettings.ShowOfferBadge && _priceSettings.ShowOfferBadgeInLists)
            {
                AddPromoBadge(price, model);
            }

            return Task.FromResult(true);
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
    }
}
