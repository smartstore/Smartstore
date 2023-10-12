using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Services;

namespace Smartstore.Web.Models.Catalog.Mappers
{
    public abstract class PriceModelMapperBase<TFrom, TTo> : IMapper<TFrom, TTo>
        where TFrom : class
        where TTo : PriceModel
    {
        public IPriceCalculationService PriceCalculationService { get; set; }
        public ICurrencyService CurrencyService { get; set; }
        public IPriceLabelService PriceLabelService { get; set; }
        public PriceSettings PriceSettings { get; set; }
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task MapAsync(TFrom from, TTo to, dynamic parameters = null)
        {
            await MapCoreAsync(from, to, parameters);

            throw new NotImplementedException();
        }

        protected abstract Task MapCoreAsync(TFrom source, TTo model, dynamic parameters = null);

        protected void MapPriceBase(CalculatedPrice price, PriceModel model, bool mapBasePrice = true)
        {
            model.CalculatedPrice = price;
            model.FinalPrice = price.FinalPrice;
            model.Saving = price.Saving;
            model.ValidUntilUtc = price.ValidUntilUtc;
            model.ShowRetailPriceSaving = PriceSettings.ShowRetailPriceSaving;

            var product = price.Product;
            var forSummary = model is ProductSummaryPriceModel;
            // Never show retail price in grid style listings if we have a regular price already
            var canMapRetailPrice = !price.RegularPrice.HasValue || PriceSettings.AlwaysDisplayRetailPrice;

            // Display "Free" instead of 0.00
            if (PriceSettings.DisplayTextForZeroPrices && price.FinalPrice == 0 && !model.CallForPrice)
            {
                model.FinalPrice = model.FinalPrice.WithPostFormat(T("Products.Free"));
            }

            // Regular price
            if (model.RegularPrice == null && price.Saving.HasSaving && price.RegularPrice.HasValue)
            {
                model.RegularPrice = GetComparePriceModel(price.RegularPrice.Value, price.RegularPriceLabel, forSummary);
                if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
                {
                    // Change regular price label: "Regular/Lowest" --> "Instead of"
                    model.RegularPrice.Label = T("Products.Bundle.PriceWithoutDiscount.Note");
                }
            }

            // Retail price
            if (model.RetailPrice == null && price.RetailPrice.HasValue && canMapRetailPrice)
            {
                model.RetailPrice = GetComparePriceModel(price.RetailPrice.Value, price.RetailPriceLabel, forSummary);

                // Don't show saving if there is no actual discount and ShowRetailPriceSaving is FALSE
                if (model.RegularPrice == null && !model.ShowRetailPriceSaving)
                {
                    model.Saving = new PriceSaving { SavingPrice = new Money(0, price.FinalPrice.Currency) };
                }
            }

            // BasePrice (PanGV)
            model.IsBasePriceEnabled = mapBasePrice &&
                price.FinalPrice != decimal.Zero &&
                product.BasePriceEnabled &&
                !(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing);

            if (model.IsBasePriceEnabled)
            {
                model.BasePriceInfo = PriceCalculationService.GetBasePriceInfo(
                    product: product,
                    price: price.FinalPrice,
                    targetCurrency: price.FinalPrice.Currency,
                    // Don't display tax suffix in detail
                    displayTaxSuffix: forSummary ? null : false);
            }

            // Shipping surcharge
            if (product.AdditionalShippingCharge > 0)
            {
                var charge = CurrencyService.ConvertFromPrimaryCurrency(product.AdditionalShippingCharge, model.FinalPrice.Currency);
                model.ShippingSurcharge = charge.WithPostFormat(T("Common.AdditionalShippingSurcharge"));
            }
        }

        protected void AddPromoBadge(CalculatedPrice price, List<ProductBadgeModel> badges)
        {
            // Add default promo badges as configured
            var (label, style) = PriceLabelService.GetPricePromoBadge(price);

            if (label.HasValue())
            {
                badges.Add(new ProductBadgeModel
                {
                    Label = label,
                    Style = style ?? "dark",
                    DisplayOrder = 10
                });
            }
        }

        protected ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel, bool forSummary)
        {
            var model = new ComparePriceModel { Price = comparePrice };

            if (forSummary)
            {
                model.Label = priceLabel.GetLocalized(x => x.ShortName);
            }
            else
            {
                // In product detail we should fallback to ShortName if Name is empty.
                model.Label = priceLabel.GetLocalized(x => x.Name).Value.NullEmpty() ?? priceLabel.GetLocalized(x => x.ShortName);
                model.Description = priceLabel.GetLocalized(x => x.Description);
            }

            return model;
        }
    }
}
