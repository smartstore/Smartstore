using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public class ProductDetailsPriceMapper : CalculatedPriceMapper<DetailsPriceModel>
    {
        public ProductDetailsPriceMapper(IPriceLabelService labelService, PriceSettings priceSettings)
            : base(labelService, priceSettings)
        {
        }

        protected override Task MapCoreAsync(CalculatedPrice source, DetailsPriceModel model, dynamic parameters = null)
        {
            model.CountdownText = _labelService.GetPromoCountdownText(source);
            
            return Task.FromResult(model);
        }

        protected override bool ShouldMapRetailPrice(CalculatedPrice price)
        {
            return !price.RegularPrice.HasValue || _priceSettings.AlwaysDisplayRetailPrice;
        }

        protected override ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel)
        {
            return new ComparePriceModel
            {
                Price = comparePrice,
                // In product detail we should fallback to ShortName if Name is empty.
                Label = priceLabel.GetLocalized(x => x.Name).Value.NullEmpty() ?? priceLabel.GetLocalized(x => x.ShortName),
                Description = priceLabel.GetLocalized(x => x.Description)
            };
        }

        protected override void AddPromoBadges(CalculatedPrice price, DetailsPriceModel model)
        {
            // Add default badges first
            base.AddPromoBadges(price, model);

            // Then handle bundle product badge
            // TODO: (mg) (pricing) Add badge for bundle (formerly *Notes --> "Im Set nur")
        }
    }
}
