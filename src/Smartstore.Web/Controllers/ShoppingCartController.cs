using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities.Html;
using Smartstore.Web.Components;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.ShoppingCart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;
        private readonly ITaxService _taxService;
        private readonly IMediaService _mediaService;
        private readonly IActivityLogger _activityLogger;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly ICurrencyService _currencyService;
        private readonly IDiscountService _discountService;
        private readonly IGiftCardService _giftCardService;
        private readonly IDownloadService _downloadService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public ShoppingCartController(
            SmartDbContext db,
            IMessageFactory messageFactory,
            ITaxService taxService,
            IMediaService mediaService,
            IActivityLogger activityLogger,
            IPaymentService paymentService,
            IShippingService shippingService,
            ICurrencyService currencyService,
            IDiscountService discountService,
            IGiftCardService giftCardService,
            IDownloadService downloadService,
            IShoppingCartService shoppingCartService,
            ILocalizationService localizationService,
            IDeliveryTimeService deliveryTimeService,
            IPriceCalculationService priceCalculationService,
            IOrderCalculationService orderCalculationService,
            IShoppingCartValidator shoppingCartValidator,
            IProductAttributeFormatter productAttributeFormatter,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MeasureSettings measureSettings,
            CaptchaSettings captchaSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings,
            ShippingSettings shippingSettings,
            CustomerSettings customerSettings,
            RewardPointsSettings rewardPointsSettings)
        {
            _db = db;
            _messageFactory = messageFactory;
            _taxService = taxService;
            _mediaService = mediaService;
            _activityLogger = activityLogger;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _currencyService = currencyService;
            _discountService = discountService;
            _giftCardService = giftCardService;
            _downloadService = downloadService;
            _shoppingCartService = shoppingCartService;
            _localizationService = localizationService;
            _deliveryTimeService = deliveryTimeService;
            _priceCalculationService = priceCalculationService;
            _orderCalculationService = orderCalculationService;
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _measureSettings = measureSettings;
            _captchaSettings = captchaSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
            _shippingSettings = shippingSettings;
            _customerSettings = customerSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        #region Utilities

        //// TODO: (ms) (core) Move this method to ShoppingCartValidator service
        //[NonAction]
        //protected async Task<bool> ValidateAndSaveCartDataAsync(ProductVariantQuery query, List<string> warnings, bool useRewardPoints = false)
        //{
        //    Guard.NotNull(query, nameof(query));

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    // Validate checkout attributes.
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var checkoutAttributes = customer.GenericAttributes.CheckoutAttributes;
        //    var isValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings, true, checkoutAttributes);
        //    if (isValid)
        //    {
        //        // Reward points.
        //        if (_rewardPointsSettings.Enabled)
        //        {
        //            customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;
        //            await customer.GenericAttributes.SaveChangesAsync();
        //        }
        //    }

        //    return isValid;
        //}

        [NonAction]
        protected async Task ParseAndSaveCheckoutAttributesAsync(List<OrganizedShoppingCartItem> cart, ProductVariantQuery query)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(query, nameof(query));

            var selectedAttributes = new CheckoutAttributeSelection(string.Empty);
            var customer = cart.GetCustomer() ?? Services.WorkContext.CurrentCustomer;

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

            foreach (var attribute in checkoutAttributes)
            {
                var selectedItems = query.CheckoutAttributes.Where(x => x.AttributeId == attribute.Id);

                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Boxes:
                        {
                            var selectedValue = selectedItems.FirstOrDefault()?.Value;
                            if (selectedValue.HasValue())
                            {
                                var selectedAttributeValueId = selectedValue.SplitSafe(",").FirstOrDefault()?.ToInt();
                                if (selectedAttributeValueId.GetValueOrDefault() > 0)
                                {
                                    selectedAttributes.AddAttributeValue(attribute.Id, selectedAttributeValueId.Value);
                                }
                            }
                        }
                        break;

                    case AttributeControlType.Checkboxes:
                        {
                            foreach (var item in selectedItems)
                            {
                                var selectedValue = item.Value.SplitSafe(",").FirstOrDefault()?.ToInt();
                                if (selectedValue.GetValueOrDefault() > 0)
                                {
                                    selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                                }
                            }
                        }
                        break;

                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
                            if (selectedValue.HasValue())
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                            }
                        }
                        break;

                    case AttributeControlType.Datepicker:
                        {
                            var selectedValue = selectedItems.FirstOrDefault()?.Date;
                            if (selectedValue.HasValue)
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue.Value);
                            }
                        }
                        break;

                    case AttributeControlType.FileUpload:
                        {
                            var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
                            if (selectedValue.HasValue())
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                            }
                        }
                        break;
                }
            }

            customer.GenericAttributes.CheckoutAttributes = selectedAttributes;
            _db.TryUpdate(customer);
            await _db.SaveChangesAsync();
        }

        [NonAction]
        protected async Task<MiniShoppingCartModel> PrepareMiniShoppingCartModelAsync()
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

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            model.TotalProducts = cart.GetTotalQuantity();

            if (cart.Count == 0)
            {
                return model;
            }

            // TODO: (ms) (core) subtotal is always 0. Check again when pricing is fully implmented.
            //model.SubTotal = (await _orderCalculationService.GetShoppingCartSubTotalAsync(cart)).SubTotalWithoutDiscount.ToString();
            model.SubTotal = "99 €";

            // A customer should visit the shopping cart page before going to checkout if:
            //1. There is at least one checkout attribute that is reqired
            //2. Min order sub total is OK

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

            model.DisplayCheckoutButton = !checkoutAttributes.Any(x => x.IsRequired);

            // Products sort descending (recently added products)
            foreach (var cartItem in cart)
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
                        false,
                        false,
                        false,
                        false,
                        false)
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
                    cartItemModel.UnitPrice = T("Products.CallForPrice");
                }
                else
                {
                    var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(item.ProductId, item.AttributeSelection);
                    product.MergeWithCombination(attributeCombination);

                    var unitPriceWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetUnitPriceAsync(cartItem, true));
                    var unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceWithDiscountBase.Price.Amount, Services.WorkContext.WorkingCurrency);

                    cartItemModel.UnitPrice = unitPriceWithDiscount.ToString();

                    if (unitPriceWithDiscount != decimal.Zero && model.ShowBasePrice)
                    {
                        cartItemModel.BasePriceInfo = await _priceCalculationService.GetBasePriceInfoAsync(item.Product);
                    }
                }

                // Image.
                if (_shoppingCartSettings.ShowProductImagesInMiniShoppingCart)
                {
                    await cartItem.MapAsync(cartItemModel.Image, _mediaSettings.MiniCartThumbPictureSize, cartItemModel.ProductName);
                }

                model.Items.Add(cartItemModel);
            }

            return model;
        }

        #endregion

        public IActionResult CartSummary()
        {
            // Stop annoying MiniProfiler report.
            return new EmptyResult();
        }

        [RequireSsl]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return RedirectToRoute("Homepage");
            }

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Any())
            {
                await ParseAndSaveCheckoutAttributesAsync(cart, query);
            }

            var model = new ShoppingCartModel();
            await cart.AsEnumerable().MapAsync(model);

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return View(model);
        }

        [RequireSsl]
        [LocalizedRoute("/wishlist/{customerGuid:guid?}", Name = "Wishlist")]
        public async Task<IActionResult> Wishlist(Guid? customerGuid)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
            {
                return RedirectToRoute("Homepage");
            }

            var customer = customerGuid.HasValue
                ? await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerGuid == customerGuid.Value)
                : Services.WorkContext.CurrentCustomer;

            if (customer == null)
            {
                return RedirectToRoute("Homepage");
            }

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);

            var model = new WishlistModel();
            await cart.AsEnumerable().MapAsync(model);

            return View(model);
        }

        #region Offcanvas

        public async Task<IActionResult> OffCanvasShoppingCart()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
            {
                return Content(string.Empty);
            }

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return Content(string.Empty);
            }

            var model = await PrepareMiniShoppingCartModelAsync();

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return PartialView(model);
        }

        public async Task<IActionResult> OffCanvasWishlist()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var cartItems = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, storeId);

            var model = new WishlistModel();
            await cartItems.AsEnumerable().MapAsync(model);

            model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

            return PartialView(model);
        }

        #endregion

        #region Shopping Cart

        ///// <summary>
        ///// Validates and saves cart data.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="useRewardPoints">A value indicating whether to use reward points.</param>        
        //[HttpPost]
        //public async Task<IActionResult> SaveCartData(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    var warnings = new List<string>();
        //    var success = await ValidateAndSaveCartDataAsync(query, warnings, useRewardPoints);

        //    return Json(new
        //    {
        //        success,
        //        message = string.Join(Environment.NewLine, warnings)
        //    });
        //}

        /// <summary>
        /// Updates cart item quantity in shopping cart.
        /// </summary>
        /// <param name="sciItemId">Identifier of <see cref="ShoppingCartItem"/>.</param>
        /// <param name="newQuantity">The new quantity to set.</param>
        /// <param name="isCartPage">A value indicating whether the customer is on the cart page or on any other page.</param>
        /// <param name="isWishlist">A value indicating whether the <see cref="ShoppingCartType"/> is Wishlist or ShoppingCart.</param>        
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(int sciItemId, int newQuantity, bool isCartPage = false, bool isWishlist = false)
        {
            // TODO: (ms) (core) Rename parameter and still retrieve value selected from input field
            // TODO: (ms) (core) Item order is beeing altered on update

            if (!await Services.Permissions.AuthorizeAsync(isWishlist ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
                return RedirectToRoute("Homepage");

            var warnings = new List<string>();
            warnings.AddRange(
                await _shoppingCartService.UpdateCartItemAsync(
                    Services.WorkContext.CurrentCustomer,
                    sciItemId,
                    newQuantity,
                    false));

            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;

            var cart = await _shoppingCartService.GetCartItemsAsync(
                null,
                isWishlist ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart,
                Services.StoreContext.CurrentStore.Id);

            if (isCartPage)
            {
                if (isWishlist)
                {
                    var model = new WishlistModel();
                    await cart.AsEnumerable().MapAsync(model);
                    cartHtml = await this.InvokeViewAsync("WishlistItems", model);
                }
                else
                {
                    var model = new ShoppingCartModel();
                    await cart.AsEnumerable().MapAsync(model);
                    cartHtml = await this.InvokeViewAsync("CartItems", model);

                    // TODO: (ms) (core) InvokeViewComponentAsync "OrderTotals" returns string.empty ?
                    totalsHtml = await this.InvokeViewComponentAsync("OrderTotals", ViewData, new { isEditable = true });
                }
            }

            // TODO: (ms) (core) subtotal is always 0. Check again when pricing was fully implmented.
            //var subTotal = await _orderCalculationService.GetShoppingCartSubTotalAsync(cart);
            var subTotal = "99 €";

            return Json(new
            {
                success = !warnings.Any(),
                SubTotal = subTotal,// subTotal.SubTotalWithoutDiscount.ToString(),
                message = warnings,
                cartHtml,
                totalsHtml,
                displayCheckoutButtons = true
            });
        }

        /// <summary>
        /// Removes cart item with identifier <paramref name="cartItemId"/> from either the shopping cart or the wishlist.
        /// </summary>
        /// <param name="cartItemId">Identifier of <see cref="ShoppingCartItem"/> to remove.</param>
        /// <param name="isWishlistItem">A value indicating whether to remove the cart item from wishlist or shopping cart.</param>        
        [HttpPost]
        public async Task<IActionResult> DeleteCartItem(int cartItemId, bool isWishlistItem = false)
        {
            if (!await Services.Permissions.AuthorizeAsync(isWishlistItem ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
            {
                return Json(new { success = false, displayCheckoutButtons = true });
            }

            // Get shopping cart item.
            var customer = Services.WorkContext.CurrentCustomer;
            var cartType = isWishlistItem ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart;
            var item = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == cartItemId && x.ShoppingCartType == cartType);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    displayCheckoutButtons = true,
                    message = T("ShoppingCart.DeleteCartItem.Failed").Value
                });
            }

            // Remove the cart item.
            await _shoppingCartService.DeleteCartItemsAsync(new[] { item }, removeInvalidCheckoutAttributes: true);

            var storeId = Services.StoreContext.CurrentStore.Id;
            // Create updated cart model.
            var cart = await _shoppingCartService.GetCartItemsAsync(cartType: cartType, storeId: storeId);
            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;

            if (cartType == ShoppingCartType.Wishlist)
            {
                var model = new WishlistModel();
                await cart.AsEnumerable().MapAsync(model);

                cartHtml = await this.InvokeViewAsync("WishlistItems", model);
            }
            else
            {
                var model = new ShoppingCartModel();
                await cart.AsEnumerable().MapAsync(model);

                cartHtml = await this.InvokeViewAsync("CartItems", model);

                // TODO: (ms) (core) InvokeViewComponentAsync "OrderTotals" returns string.empty ?
                totalsHtml = await this.InvokeViewComponentAsync("OrderTotals", ViewData, new { isEditable = true });
            }

            // Updated cart.
            return Json(new
            {
                success = true,
                displayCheckoutButtons = true,
                message = T("ShoppingCart.DeleteCartItem.Success").Value,
                cartHtml,
                totalsHtml,
                cartItemCount = cart.Count
            });
        }

        //// TODO: (ms) (core) Maybe we should rename this to AddProductToCartAjax > discuss with mc.
        //// TODO: (ms) (core) Add dev docu to all ajax action methods
        /// <summary>
        /// Adds a product without variants to the cart or redirects user to product details page.
        /// This method is used in product lists on catalog pages (category/manufacturer etc...).
        /// </summary>
        [HttpPost]
        [LocalizedRoute("/cart/addproductsimple/{productId:int}", Name = "AddProductToCartSimple")]
        public async Task<IActionResult> AddProductSimple(int productId, int shoppingCartTypeId = 1, bool forceRedirection = false)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = T("Products.NotFound", productId)
                });
            }

            // Filter out cases where a product cannot be added to the cart.
            if (product.ProductType == ProductType.GroupedProduct || product.CustomerEntersPrice || product.IsGiftCard)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
                });
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Any())
            {
                // The user must select a quantity from the dropdown list, therefore the product cannot be added to the cart.
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
                });
            }

            var storeId = Services.StoreContext.CurrentStore.Id;
            var cartType = (ShoppingCartType)shoppingCartTypeId;

            // Get existing shopping cart items. Then, try to find a cart item with the corresponding product.
            var cart = await _shoppingCartService.GetCartItemsAsync(null, cartType, storeId);
            var cartItem = cart.FindItemInCart(cartType, product);

            var quantityToAdd = product.OrderMinimumQuantity > 0 ? product.OrderMinimumQuantity : 1;

            // If we already have the same product in the cart, then use the total quantity to validate.
            quantityToAdd = cartItem != null ? cartItem.Item.Quantity + quantityToAdd : quantityToAdd;

            // Product looks good so far, let's try adding the product to the cart (with product attribute validation etc.).
            var addToCartContext = new AddToCartContext
            {
                Item = cartItem?.Item,
                Product = product,
                CartType = cartType,
                Quantity = quantityToAdd,
                AutomaticallyAddRequiredProducts = true
            };

            if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
            {
                // Item could not be added to the cart. Most likely, the customer has to select something on the product detail page e.g. variant attributes, giftcard infos, etc..
                return Json(new
                {
                    redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
                });
            }

            // Product has been added to the cart. Add to activity log.
            _activityLogger.LogActivity("PublicStore.AddToShoppingCart", T("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

            if (_shoppingCartSettings.DisplayCartAfterAddingProduct || forceRedirection)
            {
                // Redirect to the shopping cart page.
                return Json(new
                {
                    redirect = Url.RouteUrl("ShoppingCart"),
                });
            }

            return Json(new
            {
                success = true,
                message = T("Products.ProductHasBeenAddedToTheCart", Url.RouteUrl("ShoppingCart")).Value
            });
        }

        [HttpPost]
        [LocalizedRoute("/cart/addproduct/{productId:int}/{shoppingCartTypeId:int}", Name = "AddProductToCart")]
        public async Task<IActionResult> AddProduct(int productId, int shoppingCartTypeId, ProductVariantQuery query)
        {
            // TODO: (ms) (core) Redirect to product details page if product has selectable variants.

            // Adds a product to cart. This method is used on product details page.
            var form = HttpContext.Request.Form;
            var product = await _db.Products.FindByIdAsync(productId);
            if (product == null)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Homepage"),
                });
            }

            var customerEnteredPriceConverted = new Money();
            if (product.CustomerEntersPrice)
            {
                foreach (var formKey in form.Keys)
                {
                    if (formKey.EqualsNoCase($"addtocart_{productId}.CustomerEnteredPrice"))
                    {
                        if (decimal.TryParse(form[formKey], out var customerEnteredPrice))
                        {
                            customerEnteredPriceConverted = _currencyService.ConvertToPrimaryCurrency(new Money(customerEnteredPrice, Services.WorkContext.WorkingCurrency));
                        }

                        break;
                    }
                }
            }

            var quantity = product.OrderMinimumQuantity;
            var key1 = $"addtocart_{productId}.EnteredQuantity";
            var key2 = $"addtocart_{productId}.AddToCart.EnteredQuantity";

            if (form.Keys.Contains(key1))
            {
                _ = int.TryParse(form[key1], out quantity);
            }
            else if (form.Keys.Contains(key2))
            {
                _ = int.TryParse(form[key2], out quantity);
            }

            // Save item
            var cartType = (ShoppingCartType)shoppingCartTypeId;

            var addToCartContext = new AddToCartContext
            {
                Product = product,
                VariantQuery = query,
                CartType = cartType,
                CustomerEnteredPrice = customerEnteredPriceConverted,
                Quantity = quantity,
                AutomaticallyAddRequiredProducts = true
            };

            if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
            {
                // Product could not be added to the cart/wishlist
                // Display warnings.
                return Json(new
                {
                    success = false,
                    message = addToCartContext.Warnings.ToArray()
                });
            }

            // Product was successfully added to the cart/wishlist.
            // Log activity and redirect if enabled.

            bool redirect;
            string routeUrl, activity, resourceName;

            switch (cartType)
            {
                case ShoppingCartType.Wishlist:
                    {
                        redirect = _shoppingCartSettings.DisplayWishlistAfterAddingProduct;
                        routeUrl = "Wishlist";
                        activity = "PublicStore.AddToWishlist";
                        resourceName = "ActivityLog.PublicStore.AddToWishlist";
                        break;
                    }
                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        redirect = _shoppingCartSettings.DisplayCartAfterAddingProduct;
                        routeUrl = "ShoppingCart";
                        activity = "PublicStore.AddToShoppingCart";
                        resourceName = "ActivityLog.PublicStore.AddToShoppingCart";
                        break;
                    }
            }

            _activityLogger.LogActivity(activity, T(resourceName), product.Name);

            return redirect
                ? Json(new
                {
                    redirect = Url.RouteUrl(routeUrl),
                })
                : Json(new
                {
                    success = true
                });
        }

        /// <summary>
        /// Moves item from either Wishlist to ShoppingCart or vice versa.
        /// </summary>
        /// <param name="cartItemId">The identifier of <see cref="OrganizedShoppingCartItem"/>.</param>
        /// <param name="cartType">The <see cref="ShoppingCartType"/> from which to move the item from.</param>
        /// <param name="isCartPage">A value indicating whether the user is on cart page (prepares model).</param>        
        [HttpPost]
        [ActionName("MoveItemBetweenCartAndWishlist")]
        public async Task<IActionResult> MoveItemBetweenCartAndWishlistAjax(int cartItemId, ShoppingCartType cartType, bool isCartPage = false)
        {
            // TODO: (ms) (core) Investigate error message for: Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException:
            // Database operation expected to affect 1 row(s) but actually affected 0 row(s). Data may have been modified or deleted since entities were loaded.
            // See http://go.microsoft.com/fwlink/?LinkId=527962 for information on understanding and handling optimistic concurrency exceptions.

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
                || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
            {
                return Json(new
                {
                    success = false,
                    message = T("Common.NoProcessingSecurityIssue").Value
                });
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;
            var cart = await _shoppingCartService.GetCartItemsAsync(customer, cartType, storeId);
            var cartItem = cart.Where(x => x.Item.Id == cartItemId).FirstOrDefault();

            if (cartItem == null)
            {
                return Json(new
                {
                    success = false,
                    message = T("Products.ProductNotAddedToTheCart").Value
                });
            }

            var addToCartContext = new AddToCartContext
            {
                Customer = customer,
                CartType = cartType == ShoppingCartType.Wishlist ? ShoppingCartType.ShoppingCart : ShoppingCartType.Wishlist,
                StoreId = storeId,
                AutomaticallyAddRequiredProducts = true,
                Product = cartItem.Item.Product,
                RawAttributes = cartItem.Item.RawAttributes,
                CustomerEnteredPrice = new(cartItem.Item.CustomerEnteredPrice, Services.WorkContext.WorkingCurrency),
                Quantity = cartItem.Item.Quantity,
                ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
                BundleItem = cartItem.Item.BundleItem
            };

            var isValid = await _shoppingCartService.CopyAsync(addToCartContext);

            if (_shoppingCartSettings.MoveItemsFromWishlistToCart && isValid)
            {
                // No warnings (already in cart). Let's remove the item from origin.
                await _shoppingCartService.DeleteCartItemsAsync(new[] { cartItem.Item });
            }

            if (!isValid)
            {
                return Json(new
                {
                    success = false,
                    message = T("Products.ProductNotAddedToTheCart").Value
                });
            }

            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;
            var message = string.Empty;

            if (_shoppingCartSettings.DisplayCartAfterAddingProduct && cartType == ShoppingCartType.Wishlist)
            {
                // Redirect to the shopping cart page.
                return Json(new
                {
                    redirect = Url.RouteUrl("ShoppingCart")
                });
            }

            if (isCartPage)
            {
                if (cartType == ShoppingCartType.Wishlist)
                {
                    var model = new WishlistModel();
                    await cart.AsEnumerable().MapAsync(model);

                    cartHtml = await this.InvokeViewAsync("WishlistItems", model);
                    message = T("Products.ProductHasBeenAddedToTheCart");
                }
                else
                {
                    var model = new ShoppingCartModel();
                    await cart.AsEnumerable().MapAsync(model);

                    cartHtml = await this.InvokeViewAsync("CartItems", model);

                    // TODO: (ms) (core) InvokeViewComponentAsync "OrderTotals" returns string.empty ?
                    totalsHtml = await this.InvokeViewComponentAsync("OrderTotals", ViewData, new { isEditable = true });
                    message = T("Products.ProductHasBeenAddedToTheWishlist");
                }
            }

            return Json(new
            {
                success = true,
                wasMoved = _shoppingCartSettings.MoveItemsFromWishlistToCart,
                message,
                cartHtml,
                totalsHtml,
                cart.Count,
                displayCheckoutButtons = true
            });
        }

        //[HttpPost, ActionName("Wishlist")]
        //[FormValueRequired("addtocartbutton")]
        //public async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid)
        //{
        //    if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
        //        || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //    {
        //        return RedirectToRoute("Homepage");
        //    }

        //    var pageCustomer = !customerGuid.HasValue
        //        ? Services.WorkContext.CurrentCustomer
        //        : await _db.Customers
        //            .AsNoTracking()
        //            .Where(x => x.CustomerGuid == customerGuid)
        //            .FirstOrDefaultAsync();

        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var pageCart = await _shoppingCartService.GetCartItemsAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);

        //    var allWarnings = new List<string>();
        //    var numberOfAddedItems = 0;
        //    var form = HttpContext.Request.Form;

        //    var allIdsToAdd = form["addtocart"].FirstOrDefault() != null
        //        ? form["addtocart"].Select(x => int.Parse(x)).ToList()
        //        : new List<int>();

        //    foreach (var cartItem in pageCart)
        //    {
        //        if (allIdsToAdd.Contains(cartItem.Item.Id))
        //        {
        //            var addToCartContext = new AddToCartContext()
        //            {
        //                Item = cartItem.Item,
        //                Customer = Services.WorkContext.CurrentCustomer,
        //                CartType = ShoppingCartType.ShoppingCart,
        //                StoreId = storeId,
        //                RawAttributes = cartItem.Item.RawAttributes,
        //                ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
        //                CustomerEnteredPrice = new Money(cartItem.Item.CustomerEnteredPrice, _currencyService.PrimaryCurrency),
        //                Product = cartItem.Item.Product,
        //                Quantity = cartItem.Item.Quantity
        //            };

        //            if (await _shoppingCartService.CopyAsync(addToCartContext))
        //            {
        //                numberOfAddedItems++;
        //            }

        //            if (_shoppingCartSettings.MoveItemsFromWishlistToCart && !customerGuid.HasValue && addToCartContext.Warnings.Count == 0)
        //            {
        //                await _shoppingCartService.DeleteCartItemsAsync(new[] { cartItem.Item });
        //            }

        //            allWarnings.AddRange(addToCartContext.Warnings);
        //        }
        //    }

        //    if (numberOfAddedItems > 0)
        //    {
        //        return RedirectToRoute("ShoppingCart");
        //    }

        //    var cart = await _shoppingCartService.GetCartItemsAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);
        //    var model = PrepareWishlistModelAsync(cart, !customerGuid.HasValue);

        //    NotifyInfo(T("Products.SelectProducts"), true);

        //    return View(model);
        //}


        [RequireSsl]
        [GdprConsent]
        public async Task<IActionResult> EmailWishlist()
        {
            if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
                return RedirectToRoute("Homepage");

            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
            if (!cart.Any())
            {
                return RedirectToRoute("Homepage");
            }
            
            var model = new WishlistEmailAFriendModel
            {
                YourEmailAddress = customer.Email,
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailWishlistToFriendPage
            };

            return View(model);
        }

        [HttpPost, ActionName("EmailWishlist")]
        [FormValueRequired("send-email")]
        [ValidateCaptcha]
        [GdprConsent]
        public async Task<IActionResult> EmailWishlistSend(WishlistEmailAFriendModel model, string captchaError)
        {
            if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
                return RedirectToRoute("Homepage");

            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
            if (cart.Count == 0)
            {
                return RedirectToRoute("Homepage");
            }

            if (_captchaSettings.ShowOnEmailWishlistToFriendPage && captchaError.HasValue())
            {
                ModelState.AddModelError("", captchaError);
            }

            // Check whether the current customer is guest and is allowed to email wishlist.
            if (customer.IsGuest() && !_shoppingCartSettings.AllowAnonymousUsersToEmailWishlist)
            {
                ModelState.AddModelError("", T("Wishlist.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (!ModelState.IsValid)
            {
                // If we got this far, something failed, redisplay form.
                ModelState.AddModelError("", T("Common.Error.Sendmail"));
                model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailWishlistToFriendPage;

                return View(model);
            }

            await _messageFactory.SendShareWishlistMessageAsync(
                customer,
                model.YourEmailAddress,
                model.FriendEmail,
                HtmlUtils.ConvertPlainTextToHtml(model.PersonalMessage.HtmlEncode()));

            model.SuccessfullySent = true;
            model.Result = T("Wishlist.EmailAFriend.SuccessfullySent");

            // TODO: (ms) (core) Make sure that the wishlist template works as intended

            return View(model);
        }

        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("estimateshipping")]
        //public async Task<IActionResult> GetEstimateShipping(EstimateShippingModel shippingModel, ProductVariantQuery query)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart, setEstimateShippingDefaultAddress: false);

        //    model.EstimateShipping.CountryId = shippingModel.CountryId;
        //    model.EstimateShipping.StateProvinceId = shippingModel.StateProvinceId;
        //    model.EstimateShipping.ZipPostalCode = shippingModel.ZipPostalCode;

        //    if (cart.IsShippingRequired())
        //    {
        //        var shippingInfoUrl = Url.TopicAsync("ShippingInfo").ToString();
        //        if (shippingInfoUrl.HasValue())
        //        {
        //            model.EstimateShipping.ShippingInfoUrl = shippingInfoUrl;
        //        }

        //        var address = new Address
        //        {
        //            CountryId = shippingModel.CountryId,
        //            Country = await _db.Countries.FindByIdAsync(shippingModel.CountryId.GetValueOrDefault(), false),
        //            StateProvinceId = shippingModel.StateProvinceId,
        //            StateProvince = await _db.StateProvinces.FindByIdAsync(shippingModel.StateProvinceId.GetValueOrDefault(), false),
        //            ZipPostalCode = shippingModel.ZipPostalCode,
        //        };

        //        var getShippingOptionResponse = _shippingService.GetShippingOptions(cart, address, storeId: storeId);
        //        if (!getShippingOptionResponse.Success)
        //        {
        //            foreach (var error in getShippingOptionResponse.Errors)
        //            {
        //                model.EstimateShipping.Warnings.Add(error);
        //            }
        //        }
        //        else
        //        {
        //            if (getShippingOptionResponse.ShippingOptions.Count > 0)
        //            {
        //                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(storeId: storeId);

        //                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
        //                {
        //                    var soModel = new EstimateShippingModel.ShippingOptionModel
        //                    {
        //                        ShippingMethodId = shippingOption.ShippingMethodId,
        //                        Name = shippingOption.Name,
        //                        Description = shippingOption.Description
        //                    };

        //                    var currency = Services.WorkContext.WorkingCurrency;

        //                    var shippingTotal = await _orderCalculationService.AdjustShippingRateAsync(
        //                        cart,
        //                        new(shippingOption.Rate, currency),
        //                        shippingOption,
        //                        shippingMethods);

        //                    var rate = await _taxService.GetShippingPriceAsync(shippingTotal.Amount);
        //                    soModel.Price = rate.Price.ToString(true);

        //                    model.EstimateShipping.ShippingOptions.Add(soModel);
        //                }
        //            }
        //            else
        //            {
        //                model.EstimateShipping.Warnings.Add(T("Checkout.ShippingIsNotAllowed"));
        //            }
        //        }
        //    }

        //    return View(model);
        //}

        //#endregion

        //#region Upload

        //[HttpPost]
        //[MaxMediaFileSize]
        //public async Task<IActionResult> UploadFileProductAttribute(int productId, int productAttributeId, IFormFile formFile)
        //{
        //    var product = await _db.Products.FindByIdAsync(productId, false);
        //    if (product == null || formFile == null || !product.Published || product.Deleted || product.IsSystemProduct)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty,
        //        });
        //    }

        //    // Ensure that this attribute belongs to this product and has the "file upload" type
        //    var pva = await _db.ProductVariantAttributes
        //        .AsNoTracking()
        //        .ApplyProductFilter(new[] { productId })
        //        .Include(x => x.ProductAttribute)
        //        .Where(x => x.ProductAttributeId == productAttributeId)
        //        .FirstOrDefaultAsync();

        //    if (pva == null || pva.AttributeControlType != AttributeControlType.FileUpload)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty,
        //        });
        //    }

        //    var download = new Download
        //    {
        //        DownloadGuid = Guid.NewGuid(),
        //        UseDownloadUrl = false,
        //        DownloadUrl = "",
        //        UpdatedOnUtc = DateTime.UtcNow,
        //        EntityId = productId,
        //        EntityName = "ProductAttribute",
        //        IsTransient = true
        //    };

        //    var mediaFile = await _downloadService.InsertDownloadAsync(download, formFile.OpenReadStream(), formFile.FileName);

        //    return Json(new
        //    {
        //        id = download.MediaFileId,
        //        name = mediaFile.Name,
        //        type = mediaFile.MediaType,
        //        thumbUrl = _mediaService.GetUrl(download.MediaFile, _mediaSettings.ProductThumbPictureSize, string.Empty),
        //        success = true,
        //        message = T("ShoppingCart.FileUploaded").Value,
        //        downloadGuid = download.DownloadGuid,
        //    });
        //}

        //[HttpPost]
        //[MaxMediaFileSize]
        //// TODO: (ms) (core) TEST that IFormFile is beeing
        //public async Task<IActionResult> UploadFileCheckoutAttribute(IFormFile formFile)
        //{
        //    if (formFile == null || !formFile.FileName.HasValue())
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty
        //        });
        //    }

        //    var download = new Download
        //    {
        //        DownloadGuid = Guid.NewGuid(),
        //        UseDownloadUrl = false,
        //        DownloadUrl = "",
        //        UpdatedOnUtc = DateTime.UtcNow,
        //        EntityId = 0,
        //        EntityName = "CheckoutAttribute",
        //        IsTransient = true
        //    };

        //    var mediaFile = await _downloadService.InsertDownloadAsync(download, formFile.OpenReadStream(), formFile.FileName);

        //    return Json(new
        //    {
        //        id = download.MediaFileId,
        //        name = mediaFile.Name,
        //        type = mediaFile.MediaType,
        //        thumbUrl = await _mediaService.GetUrlAsync(mediaFile.File.Id, _mediaSettings.ProductThumbPictureSize, host: string.Empty),
        //        success = true,
        //        message = T("ShoppingCart.FileUploaded").Value,
        //        downloadGuid = download.DownloadGuid,
        //    });
        //}

        //#endregion

        ///// <summary>
        ///// Validates and saves cart data. When valid, customer is directed to the checkout process, otherwise the customer is 
        ///// redirected back to the shopping cart.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="useRewardPoints">A value indicating whether to use reward points.</param>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("startcheckout")]
        //public async Task<IActionResult> StartCheckout(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    ShoppingCartModel model;
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var warnings = new List<string>();
        //    if (!await ValidateAndSaveCartDataAsync(query, warnings, useRewardPoints))
        //    {
        //        // Something is wrong with the checkout data. Redisplay shopping cart.
        //        var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);
        //        model = await PrepareShoppingCartModelAsync(cart, validateCheckoutAttributes: true);
        //        return View(model);
        //    }

        //    //savechanges

        //    // Everything is OK.
        //    if (customer.IsGuest())
        //    {
        //        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
        //        {
        //            return RedirectToAction("BillingAddress", "Checkout");
        //        }
        //        else if (_orderSettings.AnonymousCheckoutAllowed)
        //        {
        //            return RedirectToRoute("Login", new { checkoutAsGuest = true, returnUrl = Url.RouteUrl("ShoppingCart") });
        //        }
        //        else
        //        {
        //            return new UnauthorizedResult();
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToRoute("Checkout");
        //    }
        //}

        ///// <summary>
        ///// Redirects customer back to last visited shopping page.
        ///// </summary>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("continueshopping")]
        //public ActionResult ContinueShopping()
        //{
        //    var returnUrl = Services.WorkContext.CurrentCustomer.GenericAttributes.LastContinueShoppingPage;
        //    return RedirectToReferrer(returnUrl);
        //}

        //#region Discount/GiftCard coupon codes & Reward points

        ///// <summary>
        ///// Tries to apply <paramref name="discountCouponcode"/> as <see cref="Discount"/> and applies 
        ///// selected checkout attributes.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/></param>
        ///// <param name="discountCouponcode">The <see cref="Discount.CouponCode"/> to apply.</param>
        ///// <returns></returns>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applydiscountcouponcode")]
        //public async Task<IActionResult> ApplyDiscountCoupon(ProductVariantQuery query, string discountCouponcode)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);
        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.DiscountBox.IsWarning = true;

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    if (discountCouponcode.HasValue())
        //    {
        //        var discount = await _db.Discounts
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.CouponCode == discountCouponcode);

        //        var isDiscountValid = discount != null
        //            && discount.RequiresCouponCode
        //            && await _discountService.IsDiscountValidAsync(discount, customer, discountCouponcode);

        //        if (isDiscountValid)
        //        {
        //            var discountApplied = true;
        //            var oldCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

        //            customer.GenericAttributes.DiscountCouponCode = discountCouponcode;

        //            if (oldCartTotal.Total.HasValue)
        //            {
        //                var newCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
        //                discountApplied = oldCartTotal.Total != newCartTotal.Total;
        //            }

        //            if (discountApplied)
        //            {
        //                model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.Applied");
        //                model.DiscountBox.IsWarning = false;
        //            }
        //            else
        //            {
        //                model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.NoMoreDiscount");

        //                customer.GenericAttributes.DiscountCouponCode = null;
        //            }
        //        }
        //        else
        //        {
        //            model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
        //        }
        //    }
        //    else
        //    {
        //        model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
        //    }

        //    return View(model);
        //}

        ///// <summary>
        ///// Removes the applied discount coupon code from current customer.
        ///// </summary>
        //[HttpPost]
        //public async Task<IActionResult> RemoveDiscountCoupon()
        //{
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    var customer = Services.WorkContext.CurrentCustomer;
        //    customer.GenericAttributes.DiscountCouponCode = null;

        //    var model = await PrepareShoppingCartModelAsync(cart);

        //    var discountHtml = await this.InvokeViewAsync("_DiscountBox", model.DiscountBox);
        //    var totalsHtml = await this.InvokeViewComponentAsync(ViewData, typeof(OrderTotalsViewComponent), new { isEditable = true });

        //    // Updated cart.
        //    return Json(new
        //    {
        //        success = true,
        //        totalsHtml,
        //        discountHtml,
        //        displayCheckoutButtons = true
        //    });
        //}

        ///// <summary>
        ///// Applies gift card by coupon code to cart.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="giftCardCouponCode">The <see cref="GiftCard.GiftCardCouponCode"/> to apply.</param>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applygiftcardcouponcode")]
        //public async Task<IActionResult> ApplyGiftCard(ProductVariantQuery query, string giftCardCouponCode)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.GiftCardBox.IsWarning = true;

        //    if (!cart.ContainsRecurringItem())
        //    {
        //        if (giftCardCouponCode.HasValue())
        //        {
        //            var giftCard = await _db.GiftCards
        //                .AsNoTracking()
        //                .ApplyCouponFilter(new[] { giftCardCouponCode })
        //                .FirstOrDefaultAsync();

        //            var isGiftCardValid = giftCard != null && _giftCardService.ValidateGiftCard(giftCard, storeId);
        //            if (isGiftCardValid)
        //            {
        //                var couponCodes = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
        //                if (couponCodes.Select(x => x.Value).Contains(giftCardCouponCode))
        //                {
        //                    var giftCardCoupon = new GiftCardCouponCode(giftCardCouponCode);
        //                    couponCodes.Add(giftCardCoupon);
        //                    customer.GenericAttributes.GiftCardCouponCodes = couponCodes;
        //                }

        //                model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.Applied");
        //                model.GiftCardBox.IsWarning = false;
        //            }
        //            else
        //            {
        //                model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
        //            }
        //        }
        //        else
        //        {
        //            model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
        //        }
        //    }
        //    else
        //    {
        //        model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.DontWorkWithAutoshipProducts");
        //    }

        //    return View(model);
        //}

        ///// <summary>
        ///// Removes applied gift card by <paramref name="giftCardId"/> from customer.
        ///// </summary>
        ///// <param name="giftCardId"><see cref="GiftCard"/> identifier to remove.</param>        
        //[HttpPost]
        //public async Task<IActionResult> RemoveGiftCardCode(int giftCardId)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
        //    var model = await PrepareShoppingCartModelAsync(cart);

        //    var giftCard = await _db.GiftCards.FindByIdAsync(giftCardId, false);
        //    if (giftCard != null)
        //    {
        //        var giftCards = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
        //        var found = giftCards.Where(x => x.Value == giftCard.GiftCardCouponCode).FirstOrDefault();
        //        if (giftCards.Remove(found))
        //        {
        //            customer.GenericAttributes.GiftCardCouponCodes = giftCards;
        //        }
        //    }

        //    var totalsHtml = await this.InvokeViewComponentAsync(ViewData, "OrderTotals", new { isEditable = true });

        //    // Updated cart.
        //    return Json(new
        //    {
        //        success = true,
        //        totalsHtml,
        //        displayCheckoutButtons = true
        //    });
        //}

        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applyrewardpoints")]
        //public async Task<IActionResult> ApplyRewardPoints(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.RewardPoints.UseRewardPoints = useRewardPoints;

        //    var customer = Services.WorkContext.CurrentCustomer;
        //    customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;

        //    return View(model);
        //}

        #endregion
    }
}
