using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using static Smartstore.Web.Models.Catalog.ProductSummaryItemModel;

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
            var context = (ProductSummaryItemContext)parameters.ModelContext;
            var contextProduct = (Product)parameters.ContextProduct;
            var options = context.CalculationOptions;
            var product = price.Product;

            model.DisplayTextForZeroPrices = _priceSettings.DisplayTextForZeroPrices;

            if (product.ProductType == ProductType.GroupedProduct)
            {
                model.DisableBuyButton = true;
                model.DisableWishlistButton = true;
                model.AvailableForPreOrder = false;
            }
            else
            {
                model.DisableBuyButton = product.DisableBuyButton || !context.AllowShoppingCart || !context.AllowPrices;
                model.DisableWishlistButton = product.DisableWishlistButton || !context.AllowWishlist || !context.AllowPrices;
                model.AvailableForPreOrder = product.AvailableForPreOrder;
            }

            if (contextProduct.CallForPrice)
            {
                model.CallForPrice = true;
                model.FinalPrice = new Money(options.TargetCurrency).WithPostFormat(context.Resources["Products.CallForPrice"]);
                return Task.FromResult(false);
            }

            if (contextProduct.CustomerEntersPrice || !context.AllowPrices || _priceSettings.PriceDisplayType == PriceDisplayType.Hide)
            {
                return Task.FromResult(false);
            }

            model.ShowSavingBadge = _priceSettings.ShowSavingBadgeInLists && price.Saving.HasSaving;
            model.ShowPriceLabel = _priceSettings.ShowPriceLabelInLists;

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
