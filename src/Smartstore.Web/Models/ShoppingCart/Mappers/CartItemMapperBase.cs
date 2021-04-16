using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{
    public abstract class CartItemMapperBase<TModel> : Mapper<OrganizedShoppingCartItem, TModel>
       where TModel : CartEntityModelBase
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly MediaSettings _mediaSettings;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly Localizer T;

        protected readonly ICommonServices _services;
        protected readonly IPriceCalculationService _priceCalculationService;
        protected readonly IProductAttributeMaterializer _productAttributeMaterializer;
        protected readonly ShoppingCartSettings _shoppingCartSettings;
        protected readonly CatalogSettings _catalogSettings;

        protected CartItemMapperBase(
            SmartDbContext db,
            ICommonServices services,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeMaterializer productAttributeMaterializer,
            IShoppingCartValidator shoppingCartValidator,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            ProductUrlHelper productUrlHelper,
            Localizer t)
        {
            _db = db;
            _services = services;
            _taxService = taxService;
            _currencyService = currencyService;
            _priceCalculationService = priceCalculationService;
            _productAttributeFormatter = productAttributeFormatter;
            _productAttributeMaterializer = productAttributeMaterializer;
            _shoppingCartValidator = shoppingCartValidator;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _productUrlHelper = productUrlHelper;
            T = t;
        }

        public override async Task MapAsync(OrganizedShoppingCartItem from, TModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));

            var item = from.Item;
            var product = from.Item.Product;
            var customer = item.Customer;
            var currency = _services.WorkContext.WorkingCurrency;
            var shoppingCartType = item.ShoppingCartType;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            var productSeName = await product.GetActiveSlugAsync();

            // General model data
            to.Id = item.Id;
            to.Sku = product.Sku;
            to.ProductId = product.Id;
            to.ProductName = product.GetLocalized(x => x.Name);
            to.ProductSeName = productSeName;
            to.ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, from);
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
                to.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                    item.AttributeSelection,
                    product,
                    customer,
                    includePrices: false,
                    includeGiftCardAttributes: true,
                    includeHyperlinks: true);

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

                if (to.BundlePerItemPricing && to.BundlePerItemShoppingCart)
                {
                    var bundleItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(from, true));
                    var bundleItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(bundleItemSubTotalWithDiscountBase.Price.Amount, currency);
                    to.BundleItem.PriceWithDiscount = bundleItemSubTotalWithDiscount.ToString();
                }
            }
            else
            {
                to.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);
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

            var quantityUnit = await _db.QuantityUnits.GetQuantityUnitByIdAsync(product.QuantityUnitId ?? 0, _catalogSettings.ShowDefaultQuantityUnit);
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
                to.UnitPrice = T("Products.CallForPrice");
            }
            else
            {
                var unitPriceWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetUnitPriceAsync(from, true));
                var unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceWithDiscountBase.Price.Amount, currency);
                to.UnitPrice = unitPriceWithDiscount.ToString();
            }

            // Subtotal and discount.
            if (product.CallForPrice)
            {
                to.SubTotal = T("Products.CallForPrice");
            }
            else
            {
                var cartItemSubTotalWithDiscount = await _priceCalculationService.GetSubTotalAsync(from, true);
                var cartItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithDiscount);
                cartItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalWithDiscountBase.Price.Amount, currency);

                to.SubTotal = cartItemSubTotalWithDiscount.ToString();

                // Display an applied discount amount.
                var cartItemSubTotalWithoutDiscount = await _priceCalculationService.GetSubTotalAsync(from, false);
                var cartItemSubTotalWithoutDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithoutDiscount);
                var cartItemSubTotalDiscountBase = cartItemSubTotalWithoutDiscountBase.Price - cartItemSubTotalWithDiscountBase.Price;

                if (cartItemSubTotalDiscountBase > decimal.Zero)
                {
                    var itemDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalDiscountBase.Amount, currency);
                    to.Discount = itemDiscount.ToString();
                }
            }

            if (item.BundleItem != null)
            {
                if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    await from.MapAsync(to.Image, _mediaSettings.CartThumbBundleItemPictureSize, to.ProductName);
                }
            }
            else
            {
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    await from.MapAsync(to.Image, _mediaSettings.CartThumbPictureSize, to.ProductName);                    
                }
            }

            var itemWarnings = new List<string>();
            var isItemValid = await _shoppingCartValidator.ValidateCartItemsAsync(new[] { from }, itemWarnings);
            if (!isItemValid)
            {
                itemWarnings.Each(x => to.Warnings.Add(x));
            }
        }
    }
}
