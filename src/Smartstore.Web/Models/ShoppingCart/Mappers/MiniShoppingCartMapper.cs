using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Web.Models.Media;
using Cart = Smartstore.Core.Checkout.Cart;

namespace Smartstore.Web.Models.ShoppingCart
{
    public static partial class MiniShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this IEnumerable<OrganizedShoppingCartItem> entity, MiniShoppingCartModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    public class MiniShoppingCartModelMapper : Mapper<IEnumerable<OrganizedShoppingCartItem>, MiniShoppingCartModel>
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IProductService _productService;
        private readonly IMediaService _mediaService;
        private readonly ICurrencyService _currencyService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly OrderSettings _orderSettings;

        public MiniShoppingCartModelMapper(
            SmartDbContext db,
            ICommonServices services,
            IProductService productService,
            IMediaService mediaService,
            ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IOrderCalculationService orderCalculationService,
            IProductAttributeFormatter productAttributeFormatter,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _services = services;
            _productService = productService;
            _priceCalculationService = priceCalculationService;
            _mediaService = mediaService;
            _currencyService = currencyService;
            _orderCalculationService = orderCalculationService;
            _productAttributeFormatter = productAttributeFormatter;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _orderSettings = orderSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override void Map(IEnumerable<OrganizedShoppingCartItem> from, MiniShoppingCartModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(IEnumerable<OrganizedShoppingCartItem> from, MiniShoppingCartModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var customer = _services.WorkContext.CurrentCustomer;
            var store = _services.StoreContext.CurrentStore;

            // TODO: (mg) (core) refactor cart item model mapping.
            var cart = new Cart.ShoppingCart(customer, store.Id, from);

            to.ShowProductImages = _shoppingCartSettings.ShowProductImagesInMiniShoppingCart;
            to.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;
            to.CurrentCustomerIsGuest = customer.IsGuest();
            to.AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed;
            to.DisplayMoveToWishlistButton = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
            to.ShowBasePrice = _shoppingCartSettings.ShowBasePrice;
            to.TotalProducts = cart.GetTotalQuantity();

            if (!from.Any())
            {
                return;
            }

            var taxFormat = _currencyService.GetTaxFormat();
            var batchContext = _productService.CreateProductBatchContext(from.Select(x => x.Item.Product).ToArray(), null, customer, false);

            var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, null, batchContext);
            var lineItems = subtotal.LineItems.ToDictionarySafe(x => x.Item.Item.Id);

            var currency = _services.WorkContext.WorkingCurrency;
            var subtotalWithoutDiscount = _currencyService.ConvertFromPrimaryCurrency(subtotal.SubtotalWithoutDiscount.Amount, currency);
            to.SubTotal = subtotalWithoutDiscount.WithPostFormat(taxFormat);

            // A customer should visit the shopping cart page before going to checkout if:
            // 1. There is at least one checkout attribute that is reqired
            // 2. Min order sub total is OK

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetCheckoutAttributesAsync(cart, store.Id);
            to.DisplayCheckoutButton = !checkoutAttributes.Any(x => x.IsRequired);

            // Products sort descending (recently added products).
            foreach (var cartItem in from)
            {
                var item = cartItem.Item;
                var product = cartItem.Item.Product;
                var productSeName = await product.GetActiveSlugAsync();

                var cartItemModel = new MiniShoppingCartModel.ShoppingCartItemModel
                {
                    Id = item.Id,
                    ProductId = product.Id,
                    ProductName = product.GetLocalized(x => x.Name),
                    ShortDesc = product.GetLocalized(x => x.ShortDescription),
                    ProductSeName = productSeName,
                    EnteredQuantity = item.Quantity,
                    MaxOrderAmount = product.OrderMaximumQuantity,
                    MinOrderAmount = product.OrderMinimumQuantity,
                    QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1,
                    CreatedOnUtc = item.UpdatedOnUtc,
                    ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
                    QuantityUnitName = null,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        item.AttributeSelection,
                        product,
                        null,
                        ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false,
                        batchContext: batchContext)
                };

                if (cartItem.ChildItems != null && _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    var bundleItems = cartItem.ChildItems.Where(x =>
                        x.Item.Id != item.Id
                        && x.Item.BundleItem != null
                        && !x.Item.BundleItem.HideThumbnail);

                    foreach (var bundleItem in bundleItems)
                    {
                        var bundleItemModel = new MiniShoppingCartModel.ShoppingCartItemBundleItem
                        {
                            ProductName = bundleItem.Item.Product.GetLocalized(x => x.Name),
                            ProductSeName = await bundleItem.Item.Product.GetActiveSlugAsync(),
                        };

                        bundleItemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(
                            bundleItem.Item.ProductId,
                            bundleItemModel.ProductSeName,
                            bundleItem.Item.AttributeSelection);

                        var file = await _db.ProductMediaFiles
                            .AsNoTracking()
                            .Include(x => x.MediaFile)
                            .ApplyProductFilter(bundleItem.Item.ProductId)
                            .FirstOrDefaultAsync();

                        if (file?.MediaFile != null)
                        {
                            var fileInfo = await _mediaService.GetFileByIdAsync(file.MediaFileId, MediaLoadFlags.AsNoTracking);

                            bundleItemModel.ImageModel = new ImageModel
                            {
                                File = fileInfo,
                                ThumbSize = MediaSettings.ThumbnailSizeXxs,
                                Title = file.MediaFile.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageLinkTitleFormat", bundleItemModel.ProductName),
                                Alt = file.MediaFile.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageAlternateTextFormat", bundleItemModel.ProductName),
                                NoFallback = _catalogSettings.HideProductDefaultPictures,
                            };
                        }

                        cartItemModel.BundleItems.Add(bundleItemModel);
                    }
                }

                // Unit prices.
                if (product.CallForPrice)
                {
                    cartItemModel.UnitPrice = cartItemModel.UnitPrice.WithPostFormat(T("Products.CallForPrice"));
                }
                else if (lineItems.TryGetValue(item.Id, out var lineItem))
                {
                    var unitPrice = _currencyService.ConvertFromPrimaryCurrency(lineItem.UnitPrice.FinalPrice.Amount, currency);
                    cartItemModel.UnitPrice = unitPrice.WithPostFormat(taxFormat);

                    if (unitPrice != 0 && to.ShowBasePrice)
                    {
                        cartItemModel.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(item.Product, unitPrice, currency);
                    }
                }

                // Image.
                if (_shoppingCartSettings.ShowProductImagesInMiniShoppingCart)
                {
                    await cartItem.MapAsync(cartItemModel.Image, _mediaSettings.MiniCartThumbPictureSize, cartItemModel.ProductName);
                }

                to.Items.Add(cartItemModel);
            }
        }
    }
}
