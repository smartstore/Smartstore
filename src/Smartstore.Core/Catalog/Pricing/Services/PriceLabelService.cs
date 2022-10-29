using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public class PriceLabelService : IPriceLabelService
    {
        private readonly PriceSettings _priceSettings;

        public PriceLabelService(PriceSettings priceSettings)
        {
            _priceSettings = priceSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        
        public PriceLabel GetComparePriceLabel(Product product)
        {
            // TODO: (pricing) Implement
            // TODO: (pricing) This should NEVER return null, even if there's no record in db.
            return new PriceLabel
            {
                ShortName = "MSRP",
                Name = "Suggested retail price",
                Description = "The Suggested Retail Price (MSRP) is the suggested or recommended retail price of a product set by the manufacturer and provided by a manufacturer, supplier, or seller.",
                IsRetailPrice = true,
                DisplayShortNameInLists = true
            };
        }

        public PriceLabel GetRegularPriceLabel(Product product)
        {
            // TODO: (pricing) Implement
            // TODO: (pricing) This should NEVER return null, even if there's no record in db.
            return new PriceLabel
            {
                ShortName = "Lowest",
                Name = "Lowest recent price",
                Description = "This is the lowest price of the product in the past 30 days prior to the application of the price reduction.",
                DisplayShortNameInLists = true
            };
        }

        public (LocalizedValue<string>, string) GetPricePromoBadge(CalculatedPrice price)
        {
            // TODO: (pricing) Properly implement
            if (!_priceSettings.ShowOfferBadge || !price.PriceSaving.HasSaving)
            {
                return (null, null);
            }
            
            // TODO: (pricing) Get data from here on from offer OR applied discount.
            var endDate = price.OfferEndDateUtc;

            // TODO: (pricing) Get localized settings.
            if (endDate.HasValue)
            {
                return (new LocalizedValue<string>("Limited time deal"), _priceSettings.LimitedOfferBadgeStyle);
            }
            else
            {
                return (new LocalizedValue<string>("Deal"), _priceSettings.OfferBadgeStyle);
            }
        }
    }
}
