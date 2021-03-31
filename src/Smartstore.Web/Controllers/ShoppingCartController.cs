using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Domain;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.ShoppingCart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly IMediaService _mediaService;
        private readonly ITaxService _taxService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly CatalogSettings _catalogSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly SmartDbContext _db;

        public ShoppingCartController(
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            IPriceCalculationService priceCalculationService,
            IShoppingCartValidator shoppingCartValidator,
            IShoppingCartService shoppingCartService,
            ICurrencyService currencyService,
            IMediaService mediaService,
            ITaxService taxService,
            ShoppingCartSettings shoppingCartSettings,
            ProductUrlHelper productUrlHelper,
            CatalogSettings catalogSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings,
            SmartDbContext db)
        {
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeFormatter = productAttributeFormatter;
            _priceCalculationService = priceCalculationService;
            _shoppingCartValidator = shoppingCartValidator;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _mediaService = mediaService;
            _taxService = taxService;
            _shoppingCartSettings = shoppingCartSettings;
            _productUrlHelper = productUrlHelper;
            _catalogSettings = catalogSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
            _db = db;
        }

        [NonAction]
        protected async Task<MiniShoppingCartModel> PrepareMiniShoppingCartModel()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var model = new MiniShoppingCartModel
            {
                ShowProductImages = _shoppingCartSettings.ShowProductImagesInMiniShoppingCart,
                ThumbSize = _mediaSettings.MiniCartThumbPictureSize,
                CurrentCustomerIsGuest = customer.IsGuest(),
                AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed,
                DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                ShowBasePrice = _shoppingCartSettings.ShowBasePrice
            };

            // TODO: (ms) (core) Finish the job.

            return model;
        }


        public IActionResult CartSummary()
        {
            // Stop annoying MiniProfiler report.
            return new EmptyResult();
        }


        public ActionResult OffCanvasCart()
        {
            var model = new OffCanvasCartModel();

            if (Services.Permissions.Authorize(Permissions.System.AccessShop))
            {
                model.ShoppingCartEnabled = _shoppingCartSettings.MiniShoppingCartEnabled && Services.Permissions.Authorize(Permissions.Cart.AccessShoppingCart);
                model.WishlistEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessWishlist);
                model.CompareProductsEnabled = _catalogSettings.CompareProductsEnabled;
            }

            return PartialView(model);
        }

        public async Task<ActionResult> OffCanvasShoppingCart()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content("");

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return Content("");

            var model = PrepareMiniShoppingCartModel();

            // TODO: (ms) Session SafeSet method extension is missing.
            //HttpContext.Session.SafeSet(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return PartialView(model);
        }

        public async Task<ActionResult> OffCanvasWishlist()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var cartItems = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, storeId);

            var model = await PrepareWishlistModelAsync(cartItems, true);

            // reformat AttributeInfo: this is bad! Put this in PrepareMiniWishlistModel later.
            model.Items.Each(async x =>
            {
                // don't display QuantityUnitName in OffCanvasWishlist
                x.QuantityUnitName = null;

                var sci = cartItems.Where(c => c.Item.Id == x.Id).FirstOrDefault();

                if (sci != null)
                {
                    x.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        sci.Item.AttributeSelection,
                        sci.Item.Product,
                        null,
                        htmlEncode: false,
                        separator: ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false);
                }
            });

            model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

            return PartialView(model);
        }


        [NonAction]
        protected async Task<WishlistModel> PrepareWishlistModelAsync(IList<OrganizedShoppingCartItem> cart, bool isEditable = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var model = new WishlistModel
            {
                IsEditable = isEditable,
                EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
                DisplayAddToCart = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
            };

            if (cart.Count == 0)
                return model;

            var customer = cart.FirstOrDefault()?.Item.Customer ?? Services.WorkContext.CurrentCustomer;
            model.CustomerGuid = customer.CustomerGuid;
            model.CustomerFullname = customer.GetFullName();
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;
            model.ShowSku = _catalogSettings.ShowProductSku;
            model.DisplayShortDesc = _shoppingCartSettings.ShowShortDesc;
            model.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;

            // Cart warnings
            var warnings = new List<string>();
            var cartIsValid = await _shoppingCartValidator.ValidateCartAsync(cart, warnings);
            if (!cartIsValid)
            {
                model.Warnings.AddRange(warnings);
            }

            foreach (var item in cart)
            {
                var wishlistCartItemModel = await PrepareWishlistCartItemModelAsync(item);

                model.Items.Add(wishlistCartItemModel);
            }

            return model;
        }

        private async Task<WishlistModel.ShoppingCartItemModel> PrepareWishlistCartItemModelAsync(OrganizedShoppingCartItem cartItem)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            var item = cartItem.Item;
            var product = item.Product;
            var customer = item.Customer;
            var currency = Services.WorkContext.WorkingCurrency;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            var productSeName = await SeoExtensions.GetActiveSlugAsync(product);

            var model = new WishlistModel.ShoppingCartItemModel
            {
                Id = item.Id,
                Sku = product.Sku,
                ProductId = product.Id,
                ProductName = product.GetLocalized(x => x.Name),
                ProductSeName = productSeName,
                ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
                EnteredQuantity = item.Quantity,
                MinOrderAmount = product.OrderMinimumQuantity,
                MaxOrderAmount = product.OrderMaximumQuantity,
                QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1,
                ShortDesc = product.GetLocalized(x => x.ShortDescription),
                ProductType = product.ProductType,
                VisibleIndividually = product.Visibility != ProductVisibility.Hidden,
                CreatedOnUtc = item.UpdatedOnUtc,
                DisableBuyButton = product.DisableBuyButton,
            };

            if (item.BundleItem != null)
            {
                model.BundleItem.Id = item.BundleItem.Id;
                model.BundleItem.DisplayOrder = item.BundleItem.DisplayOrder;
                model.BundleItem.HideThumbnail = item.BundleItem.HideThumbnail;
                model.BundlePerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing;
                model.BundlePerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart;
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer,
                    includePrices: false, includeGiftCardAttributes: false, includeHyperlinks: false);

                var bundleItemName = item.BundleItem.GetLocalized(x => x.Name);
                if (bundleItemName.HasValue())
                {
                    model.ProductName = bundleItemName;
                }

                var bundleItemShortDescription = item.BundleItem.GetLocalized(x => x.ShortDescription);
                if (bundleItemShortDescription.HasValue())
                {
                    model.ShortDesc = bundleItemShortDescription;
                }

                if (model.BundlePerItemPricing && model.BundlePerItemShoppingCart)
                {
                    (var bundleItemPriceBase, var bundleItemTaxRate) = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(cartItem, true));
                    var bundleItemPrice = _currencyService.ConvertFromPrimaryCurrency(bundleItemPriceBase.Amount, currency);
                    model.BundleItem.PriceWithDiscount = bundleItemPrice.ToString();
                }
            }
            else
            {
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
            {
                model.AllowedQuantities.Add(new SelectListItem
                {
                    Text = qty.ToString(),
                    Value = qty.ToString(),
                    Selected = item.Quantity == qty
                });
            }

            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(product.QuantityUnitId ?? 0);
            if (quantityUnit != null)
            {
                model.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
            }

            if (product.IsRecurring)
            {
                model.RecurringInfo = string.Format(T("ShoppingCart.RecurringPeriod"),
                    product.RecurringCycleLength, product.RecurringCyclePeriod.GetLocalizedEnum());
            }

            if (product.CallForPrice)
            {
                model.UnitPrice = T("Products.CallForPrice");
            }
            else
            {
                var unitPriceWithDiscount = await _priceCalculationService.GetUnitPriceAsync(cartItem, true);
                var unitPriceBaseWithDiscount = await _taxService.GetProductPriceAsync(product, unitPriceWithDiscount);
                unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceBaseWithDiscount.Price.Amount, currency);

                model.UnitPrice = unitPriceWithDiscount.ToString();
            }

            // Subtotal and discount.
            if (product.CallForPrice)
            {
                model.SubTotal = T("Products.CallForPrice");
            }
            else
            {
                var cartItemSubTotalWithDiscount = await _priceCalculationService.GetSubTotalAsync(cartItem, true);
                var cartItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithDiscount);
                cartItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalWithDiscountBase.Price.Amount, currency);

                model.SubTotal = cartItemSubTotalWithDiscount.ToString();

                // Display an applied discount amount.
                var cartItemSubTotalWithoutDiscount = await _priceCalculationService.GetSubTotalAsync(cartItem, false);
                var cartItemSubTotalWithoutDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithoutDiscount);
                var cartItemSubTotalDiscountBase = cartItemSubTotalWithoutDiscountBase.Price - cartItemSubTotalWithDiscountBase.Price;

                if (cartItemSubTotalDiscountBase > decimal.Zero)
                {
                    var shoppingCartItemDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalDiscountBase.Amount, currency);
                    model.Discount = shoppingCartItemDiscount.ToString();
                }
            }

            if (item.BundleItem != null)
            {
                if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemPictureModelAsync(product, _mediaSettings.CartThumbBundleItemPictureSize, model.ProductName, item.AttributeSelection);
                }
            }
            else
            {
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemPictureModelAsync(product, _mediaSettings.CartThumbPictureSize, model.ProductName, item.AttributeSelection);
                }
            }

            var itemWarnings = new List<string>();
            var itemIsValid = await _shoppingCartValidator.ValidateCartAsync(new List<OrganizedShoppingCartItem> { cartItem }, itemWarnings);
            if (!itemIsValid)
            {
                model.Warnings.AddRange(itemWarnings);
            }

            if (cartItem.ChildItems != null)
            {
                foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    var childModel = await PrepareWishlistCartItemModelAsync(childItem);
                    model.ChildItems.Add(childModel);
                }
            }

            return model;
        }

        [NonAction]
        protected async Task<ImageModel> PrepareCartItemPictureModelAsync(Product product, int pictureSize, string productName, ProductVariantAttributeSelection attributeSelection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(attributeSelection, nameof(attributeSelection));

            MediaFileInfo file = null;
            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributeSelection);
            if (combination != null)
            {
                var fileIds = combination.GetAssignedMediaIds();
                if (fileIds?.Any() ?? false)
                {
                    file = await _mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                }
            }

            // No attribute combination image, then load product picture.
            if (file == null)
            {
                var productMediaFile = await _db.ProductMediaFiles
                    .Include(x => x.MediaFile)
                    .Where(x => x.Id == product.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (productMediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(productMediaFile.MediaFile);
                }
            }

            // Let's check whether this product has some parent "grouped" product.
            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                var productMediaFile = await _db.ProductMediaFiles
                    .Include(x => x.MediaFile)
                    .Where(x => x.Id == product.ParentGroupedProductId)
                    .OrderBy(x => x.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (productMediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(productMediaFile.MediaFile);
                }
            }

            var pm = new ImageModel
            {
                Id = file?.Id ?? 0,
                ThumbSize = pictureSize,
                Host = _mediaService.GetUrl(file, pictureSize, null, !_catalogSettings.HideProductDefaultPictures),
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Product.ImageLinkTitleFormat", productName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Product.ImageAlternateTextFormat", productName),
                File = file
            };

            return pm;
        }
    }
}
