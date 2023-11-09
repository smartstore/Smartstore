using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Models.Cart
{
    public abstract class CartItemMapperBase<TModel> : Mapper<OrganizedShoppingCartItem, TModel>
       where TModel : CartEntityModelBase
    {
        protected readonly ICommonServices _services;
        protected readonly IPriceCalculationService _priceCalculationService;
        protected readonly IProductAttributeMaterializer _productAttributeMaterializer;
        protected readonly ShoppingCartSettings _shoppingCartSettings;
        protected readonly CatalogSettings _catalogSettings;

        protected CartItemMapperBase(
            ICommonServices services,
            IPriceCalculationService priceCalculationService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
        {
            _services = services;
            _priceCalculationService = priceCalculationService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
        }

        public SmartDbContext Db { get; set; }
        public ICurrencyService CurrencyService { get; set; }
        public IShoppingCartService ShoppingCartService { get; set; }
        public IPriceLabelService PriceLabelService { get; set; }
        public IProductAttributeFormatter ProductAttributeFormatter { get; set; }
        public IShoppingCartValidator ShoppingCartValidator { get; set; }
        public MediaSettings MediaSettings { get; set; }
        public ProductUrlHelper ProductUrlHelper { get; set; }
        public PriceSettings PriceSettings { get; set; }
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override async Task MapAsync(OrganizedShoppingCartItem from, TModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var item = from.Item;
            var product = from.Item.Product;
            var customer = item.Customer;
            var store = _services.StoreContext.CurrentStore;
            var shoppingCartType = item.ShoppingCartType;
            var productSeName = await product.GetActiveSlugAsync();
            var batchContext = parameters?.BatchContext as ProductBatchContext;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            // General model data.
            to.Id = item.Id;
            to.Sku = product.Sku;
            to.ProductId = product.Id;
            to.ProductName = product.GetLocalized(x => x.Name);
            to.ProductSeName = productSeName;
            to.ProductUrl = await ProductUrlHelper.GetProductUrlAsync(productSeName, from);
            to.ShortDesc = product.GetLocalized(x => x.ShortDescription);
            to.ProductType = product.ProductType;
            to.VisibleIndividually = product.Visibility != ProductVisibility.Hidden;
            to.CreatedOnUtc = item.UpdatedOnUtc;

            await from.MapQuantityInputAsync(to);

            if (item.BundleItem != null)
            {
                to.BundleItem = new BundleItemModel
                {
                    Id = item.BundleItem.Id,
                    DisplayOrder = item.BundleItem.DisplayOrder,
                    HideThumbnail = item.BundleItem.HideThumbnail,
                    PerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing,
                    PerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart,
                    Title = item.BundleItem.BundleProduct.GetLocalized(x => x.BundleTitleText)
                };

                to.AttributeInfo = await ProductAttributeFormatter.FormatAttributesAsync(
                    item.AttributeSelection,
                    product,
                    new ProductAttributeFormatOptions { IncludePrices = false, ItemSeparator = Environment.NewLine, FormatTemplate = "<span>{0}:</span> <span>{1}</span>" },
                    customer,
                    batchContext: batchContext);

                var bundleItemName = item.BundleItem.GetLocalized(x => x.Name);
                if (bundleItemName.Value.HasValue())
                {
                    to.ProductName = bundleItemName;
                }

                var bundleItemShortDescription = item.BundleItem.GetLocalized(x => x.ShortDescription);
                if (bundleItemShortDescription.Value.HasValue())
                {
                    to.ShortDesc = bundleItemShortDescription;
                }
            }
            else
            {
                to.AttributeInfo = await ProductAttributeFormatter.FormatAttributesAsync(
                    item.AttributeSelection, 
                    product,
                    new ProductAttributeFormatOptions { ItemSeparator = Environment.NewLine, FormatTemplate = "<span>{0}:</span> <span>{1}</span>" },
                    customer, 
                    batchContext: batchContext);
            }

            if (product.IsRecurring)
            {
                to.RecurringInfo = T("ShoppingCart.RecurringPeriod", product.RecurringCycleLength, product.RecurringCyclePeriod.GetLocalizedEnum());
            }

            // Map price
            await MapPriceAsync(from, to, parameters);

            if (item.BundleItem != null)
            {
                if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    await from.MapAsync(to.Image, MediaSettings.CartThumbBundleItemPictureSize, to.ProductName);
                }
            }
            else
            {
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    await from.MapAsync(to.Image, MediaSettings.CartThumbPictureSize, to.ProductName);
                }
            }

            var itemWarnings = new List<string>();
            if (!await ShoppingCartValidator.ValidateProductAsync(from.Item, null, itemWarnings))
            {
                to.Warnings.AddRange(itemWarnings);
            }

            var cart = await ShoppingCartService.GetCartAsync(customer, shoppingCartType, store.Id);

            var attributeWarnings = new List<string>();
            if (!await ShoppingCartValidator.ValidateProductAttributesAsync(item, cart.Items, attributeWarnings))
            {
                to.Warnings.AddRange(attributeWarnings);
            }
        }

        protected async Task MapPriceAsync(OrganizedShoppingCartItem from, TModel to, dynamic parameters = null)
        {
            var item = from.Item;
            var product = item.Product;
            var customer = item.Customer;
            var currency = _services.WorkContext.WorkingCurrency;
            var shoppingCartType = item.ShoppingCartType;
            var priceModel = to.Price;
            
            var taxFormat = parameters?.TaxFormat as string;
            var batchContext = parameters?.BatchContext as ProductBatchContext;

            if (to.BundleItem != null && item.BundleItem != null)
            {
                if (to.BundleItem.PerItemPricing && to.BundleItem.PerItemShoppingCart)
                {
                    // Handle a bundle product's sub item pricing
                    var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                    var (bundleItemUnitPrice, bundleItemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                    MapCalculatedPrice(bundleItemUnitPrice, bundleItemSubtotal);
                }
            }

            if (item.BundleItem == null)
            {
                if (shoppingCartType == ShoppingCartType.ShoppingCart)
                {
                    var subtotal = parameters?.CartSubtotal as ShoppingCartSubtotal;
                    var lineItem = subtotal.LineItems.FirstOrDefault(x => x.Item.Item.Id == item.Id);

                    MapCalculatedPrice(lineItem.UnitPrice, lineItem.Subtotal);
                    priceModel.IsBundlePart = true;
                }
                else
                {
                    var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, batchContext);
                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                    var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                    MapCalculatedPrice(unitPrice, itemSubtotal);
                }
            }

            void MapCalculatedPrice(CalculatedPrice unitPrice, CalculatedPrice totalPrice)
            {
                priceModel.Quantity = to.EnteredQuantity;

                if (unitPrice.PricingType == PricingType.CallForPrice)
                {
                    priceModel.UnitPrice = unitPrice.FinalPrice;
                    priceModel.SubTotal = unitPrice.FinalPrice;
                    return;
                }

                priceModel.ValidUntilUtc = totalPrice.ValidUntilUtc;
                priceModel.ShowRetailPriceSaving = PriceSettings.ShowRetailPriceSaving;

                // Single unit price
                priceModel.UnitPrice = ConvertMoney(unitPrice.FinalPrice, taxFormat);

                // Subtotal
                priceModel.SubTotal = ConvertMoney(totalPrice.FinalPrice, taxFormat);

                // Single unit saving
                var saving = unitPrice.Saving;
                if (saving.HasSaving)
                {
                    priceModel.Saving = new PriceSaving
                    {
                        HasSaving = true,
                        SavingPercent = saving.SavingPercent,
                        SavingAmount = ConvertMoney(saving.SavingAmount.Value),
                        SavingPrice = ConvertMoney(saving.SavingPrice)
                    };
                }

                // Total discount
                if (totalPrice.DiscountAmount > 0)
                {
                    priceModel.TotalDiscount = ConvertMoney(totalPrice.DiscountAmount, taxFormat);
                }

                // Countdown text
                priceModel.CountdownText = PriceLabelService.GetPromoCountdownText(unitPrice);

                // Offer badges
                if (PriceSettings.ShowOfferBadge)
                {
                    // Add default promo badges as configured
                    AddPromoBadge(unitPrice, priceModel.Badges);
                }

                // Regular price
                if (unitPrice.Saving.HasSaving && unitPrice.RegularPrice.HasValue)
                {
                    priceModel.RegularPrice = GetComparePriceModel(ConvertMoney(unitPrice.RegularPrice.Value, taxFormat), unitPrice.RegularPriceLabel);
                    if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
                    {
                        // Change regular price label: "Regular/Lowest" --> "Instead of"
                        priceModel.RegularPrice.Label = T("Products.Bundle.PriceWithoutDiscount.Note");
                    }
                }

                // Retail price
                var canMapRetailPrice = !unitPrice.RegularPrice.HasValue || PriceSettings.AlwaysDisplayRetailPrice;
                if (canMapRetailPrice && unitPrice.RetailPrice.HasValue)
                {
                    priceModel.RetailPrice = GetComparePriceModel(ConvertMoney(unitPrice.RetailPrice.Value, taxFormat), unitPrice.RetailPriceLabel);

                    // Don't show saving if there is no actual discount and ShowRetailPriceSaving is FALSE
                    if (priceModel.RegularPrice == null && !priceModel.ShowRetailPriceSaving)
                    {
                        priceModel.Saving = new PriceSaving { SavingPrice = new Money(0, currency) };
                    }
                }

                // Single unit base price info (PanGV)
                if (
                    _shoppingCartSettings.ShowBasePrice &&
                    priceModel.UnitPrice != decimal.Zero && 
                    product.BasePriceEnabled && 
                    !(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing))
                {
                    priceModel.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(
                        product: product, 
                        price: priceModel.UnitPrice,
                        targetCurrency: priceModel.UnitPrice.Currency,
                        displayTaxSuffix: false);
                }

                // Shipping surcharge
                if (product.AdditionalShippingCharge > 0)
                {
                    var charge = CurrencyService.ConvertFromPrimaryCurrency(product.AdditionalShippingCharge, currency);
                    priceModel.ShippingSurcharge = charge.WithPostFormat(T("Common.AdditionalShippingSurcharge"));
                }
            }

            Money ConvertMoney(Money money, string postFormat = null)
            {
                return CurrencyService.ConvertFromPrimaryCurrency(money.Amount, currency).WithPostFormat(postFormat);
            }
        }

        private void AddPromoBadge(CalculatedPrice price, List<ProductBadgeModel> badges)
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

        private static ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel)
        {
            return new ComparePriceModel 
            { 
                Price = comparePrice,
                Label = priceLabel.GetLocalized(x => x.Name).Value.NullEmpty() ?? priceLabel.GetLocalized(x => x.ShortName),
                Description = priceLabel.GetLocalized(x => x.Description)
            };
        }
    }
}
