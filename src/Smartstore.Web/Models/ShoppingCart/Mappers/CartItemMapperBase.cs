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
                to.BundleItem.Id = item.BundleItem.Id;
                to.BundleItem.DisplayOrder = item.BundleItem.DisplayOrder;
                to.BundleItem.HideThumbnail = item.BundleItem.HideThumbnail;
                to.BundlePerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing;
                to.BundlePerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart;

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

            if (item.BundleItem != null)
            {
                // Handle a bundle product's sub item pricing
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                var (bundleItemUnitPrice, bundleItemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                if (to.BundlePerItemPricing && to.BundlePerItemShoppingCart)
                {
                    to.BundleItem.PriceWithDiscount = bundleItemSubtotal.FinalPrice.ToString(); // x
                    to.BundleItem.Price = bundleItemSubtotal.FinalPrice;
                }

                to.BasePrice = _priceCalculationService.GetBasePriceInfo(product, bundleItemUnitPrice.FinalPrice); // x
                priceModel.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(product, bundleItemUnitPrice.FinalPrice);
            }

            if (product.CallForPrice)
            {
                to.UnitPrice = new(0, currency, false, T("Products.CallForPrice")); // x
                to.SubTotal = to.UnitPrice; // x

                priceModel.UnitPrice = new(0, currency, false, T("Products.CallForPrice"));
                priceModel.SubTotal = priceModel.UnitPrice;
            }
            else if (item.BundleItem == null)
            {
                if (shoppingCartType == ShoppingCartType.ShoppingCart)
                {
                    var subtotal = parameters?.CartSubtotal as ShoppingCartSubtotal;
                    var lineItem = subtotal.LineItems.FirstOrDefault(x => x.Item.Item.Id == item.Id);

                    var unitPrice = CurrencyService.ConvertFromPrimaryCurrency(lineItem.UnitPrice.FinalPrice.Amount, currency);
                    to.UnitPrice = unitPrice.WithPostFormat(taxFormat); // x

                    var itemSubtotal = CurrencyService.ConvertFromPrimaryCurrency(lineItem.Subtotal.FinalPrice.Amount, currency);
                    to.SubTotal = itemSubtotal.WithPostFormat(taxFormat); // x

                    if (lineItem.Subtotal.DiscountAmount > 0)
                    {
                        var itemDiscount = CurrencyService.ConvertFromPrimaryCurrency(lineItem.Subtotal.DiscountAmount.Amount, currency);
                        to.Discount = itemDiscount.WithPostFormat(taxFormat); // x
                    }

                    to.BasePrice = _priceCalculationService.GetBasePriceInfo(product, unitPrice); // x

                    MapCalculatedPrice(lineItem.UnitPrice, lineItem.Subtotal);
                }
                else
                {
                    var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                    var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                    to.UnitPrice = unitPrice.FinalPrice; // x
                    to.SubTotal = itemSubtotal.FinalPrice; // x

                    if (itemSubtotal.DiscountAmount > 0)
                    {
                        to.Discount = itemSubtotal.DiscountAmount; // x
                    }

                    MapCalculatedPrice(unitPrice, itemSubtotal);
                }
            }

            void MapCalculatedPrice(CalculatedPrice unitPrice, CalculatedPrice totalPrice)
            {
                priceModel.ValidUntilUtc = totalPrice.ValidUntilUtc;
                priceModel.ShowRetailPriceSaving = PriceSettings.ShowRetailPriceSaving;

                priceModel.UnitPrice = CurrencyService.ConvertFromPrimaryCurrency(unitPrice.FinalPrice.Amount, currency).WithPostFormat(taxFormat);
                priceModel.SubTotal = CurrencyService.ConvertFromPrimaryCurrency(totalPrice.FinalPrice.Amount, currency).WithPostFormat(taxFormat);

                var saving = totalPrice.Saving;
                if (saving.HasSaving)
                {
                    priceModel.Saving = new PriceSaving
                    {
                        HasSaving = true,
                        SavingPercent = saving.SavingPercent,
                        SavingAmount = CurrencyService.ConvertFromPrimaryCurrency(saving.SavingAmount.Value.Amount, currency),
                        SavingPrice = CurrencyService.ConvertFromPrimaryCurrency(saving.SavingPrice.Amount, currency)
                    };
                }

                if (totalPrice.DiscountAmount > 0)
                {
                    var itemDiscount = CurrencyService.ConvertFromPrimaryCurrency(totalPrice.DiscountAmount.Amount, currency);
                    priceModel.Discount = itemDiscount.WithPostFormat(taxFormat);
                }

                priceModel.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(product, priceModel.UnitPrice);
            }
        }
    }
}
