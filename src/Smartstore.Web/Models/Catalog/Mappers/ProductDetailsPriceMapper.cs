using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public class ProductDetailsPriceMapper : CalculatedPriceMapper<DetailsPriceModel>
    {
        public ProductDetailsPriceMapper(IPriceLabelService labelService, PriceSettings priceSettings)
            : base(labelService, priceSettings)
        {
        }

        protected override Task<bool> MapCoreAsync(CalculatedPrice price, DetailsPriceModel model, dynamic parameters = null)
        {
            var modelContext = (ProductDetailsModelContext)parameters.ModelContext;
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var isBundleItemPricing = productBundleItem != null && productBundleItem.BundleProduct.BundlePerItemPricing;
            var isBundle = product.ProductType == ProductType.BundledProduct;

            model.HidePrices = !modelContext.DisplayPrices;
            model.ShowLoginNote = !modelContext.DisplayPrices && productBundleItem == null && _priceSettings.ShowLoginForPriceNote;

            if (!modelContext.DisplayPrices)
            {
                return Task.FromResult(false);
            }

            if (product.CustomerEntersPrice && !isBundleItemPricing)
            {
                model.CustomerEntersPrice = true;
                return Task.FromResult(false);
            }

            if (product.CallForPrice && !isBundleItemPricing)
            {
                model.CallForPrice = true;
                return Task.FromResult(false);
            }

            model.BundleItemShowBasePrice = _priceSettings.BundleItemShowBasePrice;
            model.CountdownText = _labelService.GetPromoCountdownText(price);
            
            if (_priceSettings.ShowOfferBadge)
            {
                AddPromoBadge(price, model);
            }

            if (isBundle && product.BundlePerItemPricing)
            {
                if (price.RegularPrice.HasValue)
                {
                    model.RegularPrice = new ComparePriceModel
                    {
                        Price = price.RegularPrice.Value,
                        Label = T("Products.Bundle.PriceWithoutDiscount.Note"),
                    };
                }
                
                if (price.Saving.HasSaving && !product.HasTierPrices)
                {
                    // Add promo badge for bundle: "As bundle only"
                    model.Badges.Add(new PriceBadgeModel
                    {
                        Label = T("Products.Bundle.PriceWithDiscount.Note"),
                        Style = "success"
                    });
                }
            }

            return Task.FromResult(true);
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
    }
}
