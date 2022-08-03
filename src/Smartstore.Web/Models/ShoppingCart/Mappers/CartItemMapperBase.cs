using Microsoft.AspNetCore.Mvc.Rendering;
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
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override async Task MapAsync(OrganizedShoppingCartItem from, TModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var item = from.Item;
            var product = from.Item.Product;
            var customer = item.Customer;
            var store = _services.StoreContext.CurrentStore;
            var currency = _services.WorkContext.WorkingCurrency;
            var shoppingCartType = item.ShoppingCartType;
            var productSeName = await product.GetActiveSlugAsync();

            var taxFormat = parameters?.TaxFormat as string;
            var batchContext = parameters?.BatchContext as ProductBatchContext;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            // General model data.
            to.Id = item.Id;
            to.Sku = product.Sku;
            to.ProductId = product.Id;
            to.ProductName = product.GetLocalized(x => x.Name);
            to.ProductSeName = productSeName;
            to.ProductUrl = await ProductUrlHelper.GetProductUrlAsync(productSeName, from);
            to.EnteredQuantity = item.Quantity;
            to.MinOrderAmount = product.OrderMinimumQuantity;
            to.MaxOrderAmount = product.OrderMaximumQuantity;
            to.QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1;
            to.ShortDesc = product.GetLocalized(x => x.ShortDescription);
            to.ProductType = product.ProductType;
            to.VisibleIndividually = product.Visibility != ProductVisibility.Hidden;
            to.CreatedOnUtc = item.UpdatedOnUtc;

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
                    customer,
                    includePrices: false,
                    includeGiftCardAttributes: true,
                    includeHyperlinks: true,
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

                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                var (bundleItemUnitPrice, bundleItemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                if (to.BundlePerItemPricing && to.BundlePerItemShoppingCart)
                {
                    to.BundleItem.PriceWithDiscount = bundleItemSubtotal.FinalPrice.ToString();
                }

                to.BasePrice = _priceCalculationService.GetBasePriceInfo(product, bundleItemUnitPrice.FinalPrice);
            }
            else
            {
                to.AttributeInfo = await ProductAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer, batchContext: batchContext);
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var quantity in allowedQuantities)
            {
                to.AllowedQuantities.Add(new SelectListItem
                {
                    Text = quantity.ToString(),
                    Value = quantity.ToString(),
                    Selected = item.Quantity == quantity
                });
            }

            var quantityUnit = await Db.QuantityUnits.GetQuantityUnitByIdAsync(product.QuantityUnitId ?? 0, _catalogSettings.ShowDefaultQuantityUnit);
            if (quantityUnit != null)
            {
                to.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
            }

            if (product.IsRecurring)
            {
                to.RecurringInfo = T("ShoppingCart.RecurringPeriod", product.RecurringCycleLength, product.RecurringCyclePeriod.GetLocalizedEnum());
            }

            if (product.CallForPrice)
            {
                to.UnitPrice = to.UnitPrice.WithPostFormat(T("Products.CallForPrice"));
                to.SubTotal = to.UnitPrice;
            }
            else if (item.BundleItem == null)
            {
                if (shoppingCartType == ShoppingCartType.ShoppingCart)
                {
                    var subtotal = parameters?.CartSubtotal as ShoppingCartSubtotal;
                    var lineItem = subtotal.LineItems.FirstOrDefault(x => x.Item.Item.Id == item.Id);

                    var unitPrice = CurrencyService.ConvertFromPrimaryCurrency(lineItem.UnitPrice.FinalPrice.Amount, currency);
                    to.UnitPrice = unitPrice.WithPostFormat(taxFormat);

                    var itemSubtotal = CurrencyService.ConvertFromPrimaryCurrency(lineItem.Subtotal.FinalPrice.Amount, currency);
                    to.SubTotal = itemSubtotal.WithPostFormat(taxFormat);

                    if (lineItem.Subtotal.DiscountAmount > 0)
                    {
                        var itemDiscount = CurrencyService.ConvertFromPrimaryCurrency(lineItem.Subtotal.DiscountAmount.Amount, currency);
                        to.Discount = itemDiscount.WithPostFormat(taxFormat);
                    }

                    to.BasePrice = _priceCalculationService.GetBasePriceInfo(product, unitPrice);
                }
                else
                {
                    var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, null, batchContext);
                    var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(from, calculationOptions);
                    var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                    to.UnitPrice = unitPrice.FinalPrice;
                    to.SubTotal = itemSubtotal.FinalPrice;

                    if (itemSubtotal.DiscountAmount > 0)
                    {
                        to.Discount = itemSubtotal.DiscountAmount;
                    }
                }
            }

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

            if (!await ShoppingCartValidator.ValidateProductAsync(from.Item, itemWarnings))
            {
                to.Warnings.AddRange(itemWarnings);
            }

            var attrWarnings = new List<string>();
            var cart = await ShoppingCartService.GetCartAsync(customer, shoppingCartType, store.Id);

            if (!await ShoppingCartValidator.ValidateProductAttributesAsync(item, cart.Items, attrWarnings))
            {
                to.Warnings.AddRange(attrWarnings);
            }
        }
    }
}
