using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class CalculatedPriceMappingExtensions
    {
        public static Task MapDetailsAsync(this CalculatedPrice price, DetailsPriceModel model, ProductDetailsModelContext modelContext)
            => MapperFactory.MapAsync(price, model, new 
            { 
                ModelContext = Guard.NotNull(modelContext, nameof(modelContext)), 
                ForListing = false 
            });

        public static Task MapSummaryAsync(this CalculatedPrice price, SummaryPriceModel model)
            => MapperFactory.MapAsync(price, model, new { ForListing = true });
    }

    public abstract class CalculatedPriceMapper<TTo> : IMapper<CalculatedPrice, TTo>
        where TTo : PriceModel
    {
        protected readonly IPriceLabelService _labelService;
        protected readonly PriceSettings _priceSettings;

        protected CalculatedPriceMapper(IPriceLabelService labelService, PriceSettings priceSettings)
        {
            _labelService = labelService;
            _priceSettings = priceSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task MapAsync(CalculatedPrice from, TTo to, dynamic parameters = null)
        {
            if (!await MapCoreAsync(from, to, parameters))
            {
                return;
            }
            
            var forListing = parameters.ForListing ?? false;
            var price = from;
            var model = to;
            
            // Regular price
            if (price.Saving.HasSaving && price.RegularPrice.HasValue)
            {
                model.RegularPrice = GetComparePriceModel(price.RegularPrice.Value, price.RegularPriceLabel);
            }

            // Retail price
            if (price.RetailPrice.HasValue && ShouldMapRetailPrice(price))
            {
                model.RetailPrice = GetComparePriceModel(price.RetailPrice.Value, price.RetailPriceLabel);
            }
        }

        protected void AddPromoBadge(CalculatedPrice price, TTo model)
        {
            // Add default promo badges as configured
            var (label, style) = _labelService.GetPricePromoBadge(price);

            if (label.HasValue())
            {
                model.Badges.Add(new PriceBadgeModel
                {
                    Label = label,
                    Style = style ?? "dark"
                });
            }
        }

        protected abstract bool ShouldMapRetailPrice(CalculatedPrice price);

        protected abstract ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel);

        protected abstract Task<bool> MapCoreAsync(CalculatedPrice price, TTo model, dynamic parameters = null);
    }
}
