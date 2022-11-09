using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public static partial class CalculatedPriceMappingExtensions
    {
        public static Task MapDetailsAsync(this CalculatedPrice price, DetailsPriceModel model, ProductDetailsModelContext context)
        {
            return MapperFactory.MapAsync(price, model, new
            {
                ModelContext = Guard.NotNull(context, nameof(context)),
                ForListing = false
            });
        }

        public static Task MapSummaryAsync(this CalculatedPrice price, Product contextProduct, SummaryPriceModel model, ProductSummaryItemContext context)
        {
            return MapperFactory.MapAsync(price, model, new
            {
                ContextProduct = Guard.NotNull(contextProduct, nameof(contextProduct)),
                ModelContext = Guard.NotNull(context, nameof(context)),
                ForListing = true
            });
        }
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
                price.FinalPrice != decimal.Zero &&
                from.Product.BasePriceEnabled &&
                !(from.Product.ProductType == ProductType.BundledProduct && from.Product.BundlePerItemPricing);

            if (model.IsBasePriceEnabled)
            {
                model.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(
                    product: from.Product, 
                    price: price.FinalPrice, 
                    targetCurrency: price.FinalPrice.Currency,
                    displayTaxSuffix: forListing ? null : false);
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
