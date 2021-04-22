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
        public ITaxService TaxService { get; set; }
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
                    var bundleItemSubTotalWithDiscountBase = await TaxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(from, true));
                    var bundleItemSubTotalWithDiscount = CurrencyService.ConvertFromPrimaryCurrency(bundleItemSubTotalWithDiscountBase.Price.Amount, currency);
                    to.BundleItem.PriceWithDiscount = bundleItemSubTotalWithDiscount.ToString();
                }
            }
            else
            {
                to.AttributeInfo = await ProductAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);
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
                to.UnitPrice = T("Products.CallForPrice");
            }
            else
            {
                var unitPriceWithDiscountBase = await TaxService.GetProductPriceAsync(product, await _priceCalculationService.GetUnitPriceAsync(from, true));
                var unitPriceWithDiscount = CurrencyService.ConvertFromPrimaryCurrency(unitPriceWithDiscountBase.Price.Amount, currency);
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
                var cartItemSubTotalWithDiscountBase = await TaxService.GetProductPriceAsync(product, cartItemSubTotalWithDiscount);
                cartItemSubTotalWithDiscount = CurrencyService.ConvertFromPrimaryCurrency(cartItemSubTotalWithDiscountBase.Price.Amount, currency);

                to.SubTotal = cartItemSubTotalWithDiscount.ToString();

                // Display an applied discount amount.
                var cartItemSubTotalWithoutDiscount = await _priceCalculationService.GetSubTotalAsync(from, false);
                var cartItemSubTotalWithoutDiscountBase = await TaxService.GetProductPriceAsync(product, cartItemSubTotalWithoutDiscount);
                var cartItemSubTotalDiscountBase = cartItemSubTotalWithoutDiscountBase.Price - cartItemSubTotalWithDiscountBase.Price;

                if (cartItemSubTotalDiscountBase > decimal.Zero)
                {
                    var itemDiscount = CurrencyService.ConvertFromPrimaryCurrency(cartItemSubTotalDiscountBase.Amount, currency);
                    to.Discount = itemDiscount.ToString();
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
            var isValid = await ShoppingCartValidator.ValidateCartItemsAsync(new[] { from }, itemWarnings);
            if (!isValid)
            {
                to.Warnings.AddRange(itemWarnings);
            }

            var cart = await ShoppingCartService.GetCartItemsAsync(customer, shoppingCartType, _services.StoreContext.CurrentStore.Id);

            var attrWarnings = new List<string>();
            isValid = await ShoppingCartValidator.ValidateProductAttributesAsync(item, cart, attrWarnings);
            if (!isValid)
            {
                to.Warnings.AddRange(attrWarnings);
            }
        }
    }
}
