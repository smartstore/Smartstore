using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Web.Bundling;
using static Smartstore.Core.Security.Permissions.Catalog;

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
        protected readonly IPriceCalculationService _priceCalculationService;
        protected readonly IPriceLabelService _labelService;
        protected readonly PriceSettings _priceSettings;

        protected CalculatedPriceMapper(
            IPriceCalculationService priceCalculationService,
            IPriceLabelService labelService,
            PriceSettings priceSettings)
        {
            _priceCalculationService = priceCalculationService;
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

            model.ShowRetailPriceSaving = _priceSettings.ShowRetailPriceSaving;

            // Regular price
            if (model.RegularPrice == null && price.Saving.HasSaving && price.RegularPrice.HasValue)
            {
                model.RegularPrice = GetComparePriceModel(price.RegularPrice.Value, price.RegularPriceLabel);
            }

            // Retail price
            if (model.RetailPrice == null && price.RetailPrice.HasValue && ShouldMapRetailPrice(price))
            {
                model.RetailPrice = GetComparePriceModel(price.RetailPrice.Value, price.RetailPriceLabel);
            }

            // BasePrice (PanGV)
            model.IsBasePriceEnabled =
                from.Product.BasePriceEnabled &&
                !(from.Product.ProductType == ProductType.BundledProduct && from.Product.BundlePerItemPricing);

            if (model.IsBasePriceEnabled)
            {
                model.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(from.Product, price.FinalPrice, price.FinalPrice.Currency);
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
