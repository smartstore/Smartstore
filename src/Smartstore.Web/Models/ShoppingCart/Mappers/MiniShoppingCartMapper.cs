using Microsoft.VisualBasic;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Web.Models.Media;

namespace Smartstore.Web.Models.Cart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this ShoppingCart cart, MiniShoppingCartModel model)
        {
            await MapperFactory.MapAsync(cart, model, null);
        }
    }

    public class MiniShoppingCartModelMapper : Mapper<ShoppingCart, MiniShoppingCartModel>
    {
        static readonly ProductAttributeFormatOptions DefaultAttributeFormatOptions = new()
        {
            IncludePrices = false,
            ItemSeparator = Environment.NewLine,
            FormatTemplate = "<b>{0}:</b> <span>{1}</span>"
        };

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
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
            ICurrencyService currencyService,
            ITaxService taxService,
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
            _currencyService = currencyService;
            _taxService = taxService;
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

        protected override void Map(ShoppingCart from, MiniShoppingCartModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, MiniShoppingCartModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var customer = _services.WorkContext.CurrentCustomer;
            var store = _services.StoreContext.CurrentStore;
            var currency = _services.WorkContext.WorkingCurrency;
            var taxFormat = _taxService.GetTaxFormat();

            to.ShowProductImages = _shoppingCartSettings.ShowProductImagesInMiniShoppingCart;
            to.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;
            to.CurrentCustomerIsGuest = customer.IsGuest();
            to.AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed;
            to.DisplayMoveToWishlistButton = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
            to.ShowBasePrice = _shoppingCartSettings.ShowBasePrice;
            to.TotalQuantity = from.GetTotalQuantity();
            to.DisplayShoppingCartButton = from.HasItems || customer.ShoppingCartItems.FilterByCartType(ShoppingCartType.ShoppingCart, store.Id, false, false).Any();

            if (!from.HasItems)
            {
                return;
            }

            var batchContext = _productService.CreateProductBatchContext(from.GetAllProducts(), null, customer, false);
            var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(from, null, batchContext);
            var lineItems = subtotal.LineItems.ToDictionarySafe(x => x.Item.Item.Id);
            var subtotalAmount = subtotal.SubtotalWithoutDiscount.Amount;
            //var subtotalAmount = 0m;

            //if (from.Items.Any(x => !x.Active))
            //{
            //    // Exclude inactive cart items from subtotal calculation.
            //    var activeItemsSubtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(new(from, from.Items.Where(x => x.Active)), null, batchContext);
            //    subtotalAmount = activeItemsSubtotal.SubtotalWithoutDiscount.Amount;
            //}
            //else
            //{
            //    subtotalAmount = subtotal.SubtotalWithoutDiscount.Amount;
            //}

            to.SubTotal = _currencyService.ConvertFromPrimaryCurrency(subtotalAmount, currency).WithPostFormat(taxFormat);

            // A customer should visit the shopping cart page before going to checkout if:
            // 1. There is at least one checkout attribute that is reqired.
            // 2. Min order subtotal is OK.
            // 3. The cart contains at least one active item.
            var checkoutAttributes = await _checkoutAttributeMaterializer.GetCheckoutAttributesAsync(from, store.Id);
            to.DisplayCheckoutButton = !checkoutAttributes.Any(x => x.IsRequired) && from.Items.Any(x => x.Active);

            // Products sort descending (recently added products).
            foreach (var cartItem in from.Items)
            {
                var item = cartItem.Item;
                var product = item.Product;
                var productSeName = await product.GetActiveSlugAsync();

                var attributesInfo = await _productAttributeFormatter.FormatAttributesAsync(
                    item.AttributeSelection,
                    product,
                    DefaultAttributeFormatOptions,
                    customer,
                    batchContext);

                var cartItemModel = new MiniShoppingCartModel.ShoppingCartItemModel
                {
                    Id = item.Id,
                    Active = cartItem.Active,
                    ProductId = product.Id,
                    ProductName = product.GetLocalized(x => x.Name),
                    ShortDesc = product.GetLocalized(x => x.ShortDescription),
                    ProductSeName = productSeName,
                    CreatedOnUtc = item.UpdatedOnUtc,
                    ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
                    AttributeInfo = attributesInfo
                };

                if (_shoppingCartSettings.ShowEssentialAttributesInMiniShoppingCart)
                {
                    cartItemModel.EssentialSpecAttributesInfo = _productAttributeFormatter.FormatSpecificationAttributes(
                        await batchContext.EssentialAttributes.GetOrLoadAsync(product.Id),
                        DefaultAttributeFormatOptions);
                }

                await cartItem.MapQuantityInputAsync(cartItemModel, mapUnitName: false);

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

                        bundleItemModel.ProductUrl = await _productUrlHelper.GetProductPathAsync(
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
                            var fileInfo = await _services.MediaService.GetFileByIdAsync(file.MediaFileId, MediaLoadFlags.AsNoTracking);

                            bundleItemModel.ImageModel = new ImageModel(fileInfo, MediaSettings.ThumbnailSizeXxs)
                            {
                                Title = file.MediaFile.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageLinkTitleFormat", bundleItemModel.ProductName),
                                Alt = file.MediaFile.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Manufacturer.ImageAlternateTextFormat", bundleItemModel.ProductName),
                                NoFallback = _catalogSettings.HideProductDefaultPictures,
                            };
                        }

                        cartItemModel.BundleItems.Add(bundleItemModel);
                    }
                }

                // Unit prices.
                if (lineItems.TryGetValue(item.Id, out var lineItem))
                {
                    if (lineItem.UnitPrice.PricingType == PricingType.CallForPrice)
                    {
                        cartItemModel.UnitPrice = lineItem.UnitPrice.FinalPrice;
                    }
                    else
                    {
                        var unitPrice = _currencyService.ConvertFromPrimaryCurrency(lineItem.UnitPrice.FinalPrice.Amount, currency);
                        cartItemModel.UnitPrice = unitPrice.WithPostFormat(taxFormat);

                        if (unitPrice != 0 && to.ShowBasePrice)
                        {
                            cartItemModel.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(item.Product, unitPrice, currency);
                        }
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
