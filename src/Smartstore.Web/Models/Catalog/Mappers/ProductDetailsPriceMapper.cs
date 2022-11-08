using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public class ProductDetailsPriceMapper : CalculatedPriceMapper<DetailsPriceModel>
    {
        public ProductDetailsPriceMapper(
            IPriceCalculationService priceCalculationService, 
            IPriceLabelService labelService, 
            PriceSettings priceSettings)
            : base(priceCalculationService, labelService, priceSettings)
        {
        }

        protected override async Task<bool> MapCoreAsync(CalculatedPrice price, DetailsPriceModel model, dynamic parameters = null)
        {
            var modelContext = (ProductDetailsModelContext)parameters.ModelContext;
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var isBundleItemPricing = productBundleItem != null && productBundleItem.BundleProduct.BundlePerItemPricing;
            var isBundle = product.ProductType == ProductType.BundledProduct;

            model.HidePrices = !modelContext.DisplayPrices;
            model.ShowLoginNote = !modelContext.DisplayPrices && productBundleItem == null && _priceSettings.ShowLoginForPriceNote;
            model.CustomerEntersPrice = product.CustomerEntersPrice && !isBundleItemPricing;
            model.CallForPrice = product.CallForPrice && !isBundleItemPricing;
            model.BundleItemShowBasePrice = _priceSettings.BundleItemShowBasePrice;

            if (!modelContext.DisplayPrices || model.CustomerEntersPrice || model.CallForPrice)
            {
                return false;
            }
            
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

            // Tier Prices
            await CreateTierPriceModelAsync(model, modelContext);

            return true;
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

        private async Task CreateTierPriceModelAsync(DetailsPriceModel model, ProductDetailsModelContext modelContext)
        {
            var product = modelContext.Product;
            var tierPrices = product.TierPrices
                .FilterByStore(modelContext.Store.Id)
                .FilterForCustomer(modelContext.Customer)
                .OrderBy(x => x.Quantity)
                .ToList()
                .RemoveDuplicatedQuantities();
            
            if (!tierPrices.Any())
            {
                return;
            }

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, modelContext.Customer, modelContext.Currency, modelContext.BatchContext);
            calculationOptions.TaxFormat = null;

            var calculationContext = new PriceCalculationContext(product, 1, calculationOptions)
            {
                AssociatedProducts = modelContext.AssociatedProducts,
                BundleItem = modelContext.ProductBundleItem
            };

            calculationContext.AddSelectedAttributes(modelContext.SelectedAttributes, product.Id, modelContext.ProductBundleItem?.Id);

            var tierPriceModels = await tierPrices
                .SelectAwait(async (tierPrice) =>
                {
                    calculationContext.Quantity = tierPrice.Quantity;

                    var price = await _priceCalculationService.CalculatePriceAsync(calculationContext);

                    var tierPriceModel = new TierPriceModel
                    {
                        Quantity = tierPrice.Quantity,
                        Price = price.FinalPrice
                    };

                    return tierPriceModel;
                })
                .AsyncToList();

            if (tierPriceModels.Count > 0)
            {
                model.TierPrices.AddRange(tierPriceModels);
            }
        }
    }
}
