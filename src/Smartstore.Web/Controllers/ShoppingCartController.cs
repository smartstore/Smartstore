using Microsoft.AspNetCore.Http;
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
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IActivityLogger _activityLogger;
        private readonly IMediaService _mediaService;
        private readonly IShippingService _shippingService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IDiscountService _discountService;
        private readonly IGiftCardService _giftCardService;
        private readonly IDownloadService _downloadService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductCompareService _productCompareService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CatalogSettings _catalogSettings;

        public ShoppingCartController(
            SmartDbContext db,
            IMessageFactory messageFactory,
            ITaxCalculator taxCalculator,
            IActivityLogger activityLogger,
            IMediaService mediaService,
            IShippingService shippingService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IDiscountService discountService,
            IGiftCardService giftCardService,
            IDownloadService downloadService,
            IShoppingCartService shoppingCartService,
            IProductCompareService productCompareService,
            IOrderCalculationService orderCalculationService,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CaptchaSettings captchaSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings,
            CustomerSettings customerSettings,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _messageFactory = messageFactory;
            _taxCalculator = taxCalculator;
            _activityLogger = activityLogger;
            _mediaService = mediaService;
            _shippingService = shippingService;
            _currencyService = currencyService;
            _taxService = taxService;
            _discountService = discountService;
            _giftCardService = giftCardService;
            _downloadService = downloadService;
            _shoppingCartService = shoppingCartService;
            _productCompareService = productCompareService;
            _orderCalculationService = orderCalculationService;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _shoppingCartSettings = shoppingCartSettings;
            _captchaSettings = captchaSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
            _customerSettings = customerSettings;
            _catalogSettings = catalogSettings;
        }

        #region Shopping cart

        [HttpPost]
        public async Task<IActionResult> CartSummary(bool cart = false, bool wishlist = false, bool compare = false)
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cartEnabled = cart && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart) && _shoppingCartSettings.MiniShoppingCartEnabled;
            var wishlistEnabled = wishlist && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
            var compareEnabled = compare && _catalogSettings.CompareProductsEnabled;
            var cartItemsCount = 0;
            var wishlistItemsCount = 0;
            var compareItemsCount = 0;

            if (cartEnabled)
            {
                var shoppingCart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                cartItemsCount = shoppingCart.Items
                    .Where(x => x.Item.ParentItemId == null)
                    .Sum(x => (int?)x.Item.Quantity) ?? 0;
            }

            if (wishlistEnabled)
            {
                var customerWishlist = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, store.Id);

                wishlistItemsCount = customerWishlist.Items
                    .Where(x => x.Item.ParentItemId == null)
                    .Sum(x => (int?)x.Item.Quantity) ?? 0;
            }

            if (compareEnabled)
            {
                compareItemsCount = await _productCompareService.CountComparedProductsAsync();
            }

            return Json(new
            {
                //CartSubTotal = subtotalFormatted,
                CartItemsCount = cartItemsCount,
                WishlistItemsCount = wishlistItemsCount,
                CompareItemsCount = compareItemsCount
            });
        }

        [RequireSsl]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return RedirectToRoute("Homepage");
            }

            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Any())
            {
                cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
                await _db.SaveChangesAsync();
            }

            var model = await cart.MapAsync();

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

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);

            var model = new WishlistModel();
            await cart.MapAsync(model, !customerGuid.HasValue);

            return View(model);
        }

        /// <summary>
        /// Validates and saves cart data. When valid, customer is directed to the checkout process, otherwise the customer is 
        /// redirected back to the shopping cart.
        /// </summary>
        /// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        /// <param name="useRewardPoints">A value indicating whether to use reward points.</param>
        [HttpPost, ActionName("Cart")]
        [FormValueRequired("startcheckout")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> StartCheckout(ProductVariantQuery query, bool useRewardPoints = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var warnings = new List<string>();

            // Save data entered on cart page.
            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);
            if (!isCartValid)
            {
                // Something is wrong with the checkout data. Redisplay shopping cart.
                var model = await cart.MapAsync(validateCheckoutAttributes: true);

                return View(model);
            }

            if (cart.Customer.IsGuest())
            {
                if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                {
                    return RedirectToAction("BillingAddress", "Checkout");
                }
                else if (_orderSettings.AnonymousCheckoutAllowed)
                {
                    return RedirectToRoute("Login", new { checkoutAsGuest = true, returnUrl = Url.RouteUrl("ShoppingCart") });
                }

                return new UnauthorizedResult();
            }

            return RedirectToRoute("Checkout");
        }

        /// <summary>
        /// Redirects customer back to last visited shopping page or the homepage.
        /// </summary>
        [HttpPost, ActionName("Cart")]
        [FormValueRequired("continueshopping")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public IActionResult ContinueShopping()
        {
            var returnUrl = Services.WorkContext.CurrentCustomer.GenericAttributes.LastContinueShoppingPage;
            return RedirectToReferrer(returnUrl);
        }

        #endregion

        #region Offcanvas cart

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

            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, storeId);

            var model = new MiniShoppingCartModel();
            await cart.MapAsync(model);

            return PartialView(model);
        }

        public async Task<IActionResult> OffCanvasWishlist()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var wishlist = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, storeId);

            var model = new WishlistModel();
            await wishlist.MapAsync(model);

            model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

            return PartialView(model);
        }

        #endregion

        #region Modify shopping cart

        /// <summary>
        /// AJAX. Saves checkout attributes, whether to use reward points and validates the shopping cart.
        /// This action method is intended for payment buttons that skip checkout and redirect on a payment provider page.
        /// </summary>
        /// <param name="query"><see cref="ProductVariantQuery"/> with selected checkout attributes.</param>
        /// <param name="useRewardPoints">A value indicating whether to use reward points. <c>null</c> if it is called from the off-canvas card.</param>
        [HttpPost]
        public async Task<IActionResult> SaveCartData(ProductVariantQuery query, bool? useRewardPoints)
        {
            var warnings = new List<string>();
            var success = await _shoppingCartService.SaveCartDataAsync(null, warnings, query, useRewardPoints);

            return Json(new { success, warnings });
        }

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
            if (!await Services.Permissions.AuthorizeAsync(isWishlist ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
            {
                return RedirectToRoute("Homepage");
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var warnings = new List<string>();
            warnings.AddRange(await _shoppingCartService.UpdateCartItemAsync(customer, sciItemId, newQuantity, false));

            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;
            var newItemPrice = string.Empty;

            var cart = await _shoppingCartService.GetCartAsync(
                customer,
                isWishlist ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart,
                Services.StoreContext.CurrentStore.Id);

            if (isCartPage)
            {
                if (isWishlist)
                {
                    var model = new WishlistModel();
                    await cart.MapAsync(model);

                    cartHtml = await InvokePartialViewAsync("WishlistItems", model);
                }
                else
                {
                    var model = await cart.MapAsync();

                    cartHtml = await InvokePartialViewAsync("CartItems", model);
                    totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

                    var sci = model.Items.Where(x => x.Id == sciItemId).FirstOrDefault();
                    newItemPrice = sci.UnitPrice.ToString();
                }
            }

            var subTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart);

            return Json(new
            {
                success = !warnings.Any(),
                SubTotal = subTotal.SubtotalWithoutDiscount.ToString(),
                message = warnings,
                cartHtml,
                totalsHtml,
                displayCheckoutButtons = true,
                newItemPrice
            });
        }

        /// <summary>
        /// Removes cart item with identifier <paramref name="cartItemId"/> from either the shopping cart or the wishlist.
        /// </summary>
        /// <param name="cartItemId">Identifier of <see cref="ShoppingCartItem"/> to remove.</param>
        /// <param name="isWishlistItem">A value indicating whether to remove the cart item from wishlist or shopping cart.</param>        
        [HttpPost]
        [SaveChanges(typeof(SmartDbContext), false)]
        public async Task<IActionResult> DeleteCartItem(int cartItemId, bool isWishlistItem = false)
        {
            if (!await Services.Permissions.AuthorizeAsync(isWishlistItem ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
            {
                return Json(new { success = false, displayCheckoutButtons = true });
            }

            // Get shopping cart item.
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;
            var cartType = isWishlistItem ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart;
            var cart = await _shoppingCartService.GetCartAsync(customer, cartType, storeId);
            var cartItem = cart.Items.FirstOrDefault(x => x.Item.Id == cartItemId);

            if (cartItem == null)
            {
                return Json(new
                {
                    success = false,
                    displayCheckoutButtons = true,
                    message = T("ShoppingCart.DeleteCartItem.Failed").Value
                });
            }

            // Remove the cart item.
            await _shoppingCartService.DeleteCartItemAsync(cartItem.Item, true, true);

            // Get updated cart model.
            cart = await _shoppingCartService.GetCartAsync(customer, cartType, storeId);
            var totalsHtml = string.Empty;

            string cartHtml;
            if (cartType == ShoppingCartType.Wishlist)
            {
                var model = new WishlistModel();
                await cart.MapAsync(model);

                cartHtml = await InvokePartialViewAsync("WishlistItems", model);
            }
            else
            {
                var model = await cart.MapAsync();

                cartHtml = await InvokePartialViewAsync("CartItems", model);
                totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });
            }

            // Updated cart.
            return Json(new
            {
                success = true,
                displayCheckoutButtons = true,
                message = T("ShoppingCart.DeleteCartItem.Success").Value,
                cartHtml,
                totalsHtml,
                cartItemCount = cart.Items.Length
            });
        }

        /// <summary>
        /// Adds a product without variants to the cart or redirects user to product details page.
        /// This method is used in product lists on catalog pages (category/manufacturer etc...).
        /// </summary>
        /// <param name="productId">Identifier of the <see cref="Product"/> to add.</param>
        /// <param name="shoppingCartTypeId"><see cref="ShoppingCartType"/> identifier. 1 = <see cref="ShoppingCartType.ShoppingCart"/>; 2 = <see cref="ShoppingCartType.Wishlist"/> </param>
        /// <param name="forceRedirection">A value indicating whether to force a redirection to the shopping cart.</param>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [SaveChanges(typeof(SmartDbContext), false)]
        [LocalizedRoute("/cart/addproductsimple/{productId:int}", Name = "AddProductToCartSimple")]
        public async Task<IActionResult> AddProductSimple(int productId, int shoppingCartTypeId = 1, bool forceRedirection = false)
        {
            var product = await _db.Products.FindByIdAsync(productId);
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

            var quantityToAdd = product.OrderMinimumQuantity > 0 ? product.OrderMinimumQuantity : 1;

            // Product looks good so far, let's try adding the product to the cart (with product attribute validation etc.).
            var addToCartContext = new AddToCartContext
            {
                Product = product,
                CartType = cartType,
                Quantity = quantityToAdd,
                AutomaticallyAddRequiredProducts = true,
                AutomaticallyAddBundleProducts = true
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
            _activityLogger.LogActivity(KnownActivityLogTypes.PublicStoreAddToShoppingCart, T("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

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

        /// <summary>
        /// Adds a product to the cart from the product details page.
        /// </summary>
        /// <param name="productId">Identifier of the <see cref="Product"/> to add.</param>
        /// <param name="shoppingCartTypeId"><see cref="ShoppingCartType"/> identifier. 1 = <see cref="ShoppingCartType.ShoppingCart"/>; 2 = <see cref="ShoppingCartType.Wishlist"/>.</param>
        /// <param name="query">The <see cref="ProductVariantQuery"/> of selected attributes.</param>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [SaveChanges(typeof(SmartDbContext), false)]
        [LocalizedRoute("/cart/addproduct/{productId:int}/{shoppingCartTypeId:int}", Name = "AddProductToCart")]
        public async Task<IActionResult> AddProduct(int productId, int shoppingCartTypeId, ProductVariantQuery query)
        {
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
                        if (ConvertUtility.TryConvert<decimal>(form[formKey].First(), out var customerEnteredPrice))
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
                AutomaticallyAddRequiredProducts = true,
                AutomaticallyAddBundleProducts = true
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
                    activity = KnownActivityLogTypes.PublicStoreAddToWishlist;
                    resourceName = "ActivityLog.PublicStore.AddToWishlist";
                    break;
                }
                case ShoppingCartType.ShoppingCart:
                default:
                {
                    redirect = _shoppingCartSettings.DisplayCartAfterAddingProduct;
                    routeUrl = "ShoppingCart";
                    activity = KnownActivityLogTypes.PublicStoreAddToShoppingCart;
                    resourceName = "ActivityLog.PublicStore.AddToShoppingCart";
                    break;
                }
            }

            _activityLogger.LogActivity(activity, T(resourceName), product.Name);

            if (redirect)
            {
                return Json(new { redirect = Url.RouteUrl(routeUrl) });
            }

            return Json(new { success = true });
        }

        /// <summary>
        /// Moves item from either Wishlist to ShoppingCart or vice versa.
        /// </summary>
        /// <param name="cartItemId">The identifier of <see cref="OrganizedShoppingCartItem"/>.</param>
        /// <param name="cartType">The <see cref="ShoppingCartType"/> from which to move the item from.</param>
        /// <param name="isCartPage">A value indicating whether the user is on cart page (prepares model).</param>        
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ActionName("MoveItemBetweenCartAndWishlist")]
        [SaveChanges(typeof(SmartDbContext), false)]
        public async Task<IActionResult> MoveItemBetweenCartAndWishlistAjax(int cartItemId, ShoppingCartType cartType, bool isCartPage = false)
        {
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
            var cart = await _shoppingCartService.GetCartAsync(customer, cartType, storeId);
            var cartItem = cart.Items.FirstOrDefault(x => x.Item.Id == cartItemId);

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
                Product = cartItem.Item.Product,
                RawAttributes = cartItem.Item.RawAttributes,
                CustomerEnteredPrice = new(cartItem.Item.CustomerEnteredPrice, Services.WorkContext.WorkingCurrency),
                Quantity = cartItem.Item.Quantity,
                BundleItem = cartItem.Item.BundleItem,
                ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList()
            };

            var isValid = await _shoppingCartService.CopyAsync(addToCartContext);

            if (_shoppingCartSettings.MoveItemsFromWishlistToCart && isValid)
            {
                // No warnings (item is already in cart). Remove the item from origin.
                await _shoppingCartService.DeleteCartItemAsync(cartItem.Item);
            }

            if (!isValid)
            {
                return Json(new
                {
                    success = false,
                    message = T("Products.ProductNotAddedToTheCart").Value
                });
            }

            if (_shoppingCartSettings.DisplayCartAfterAddingProduct && cartType == ShoppingCartType.Wishlist)
            {
                // Redirect to the shopping cart page.
                return Json(new
                {
                    redirect = Url.RouteUrl("ShoppingCart")
                });
            }

            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;
            var message = string.Empty;
            var cartItemCount = 0;

            if (isCartPage)
            {
                // Get updated cart
                cart = await _shoppingCartService.GetCartAsync(customer, cartType, storeId);
                cartItemCount = cart.Items.Length;

                if (cartType == ShoppingCartType.Wishlist)
                {
                    var model = new WishlistModel();
                    await cart.MapAsync(model);

                    cartHtml = await InvokePartialViewAsync("WishlistItems", model);
                    message = T("Products.ProductHasBeenAddedToTheCart");
                }
                else
                {
                    var model = await cart.MapAsync();

                    cartHtml = await InvokePartialViewAsync("CartItems", model);
                    totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });
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
                cartItemCount,
                displayCheckoutButtons = true
            });
        }

        [HttpPost, ActionName("Wishlist")]
        [FormValueRequired("addtocartbutton")]
        [LocalizedRoute("/wishlist/{customerGuid:guid?}", Name = "Wishlist")]
        public async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
                || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
            {
                return RedirectToRoute("Homepage");
            }

            var pageCustomer = !customerGuid.HasValue
                ? Services.WorkContext.CurrentCustomer
                : await _db.Customers
                    .AsNoTracking()
                    .Where(x => x.CustomerGuid == customerGuid)
                    .FirstOrDefaultAsync();

            var storeId = Services.StoreContext.CurrentStore.Id;
            var pageCart = await _shoppingCartService.GetCartAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);

            var allWarnings = new List<string>();
            var numberOfAddedItems = 0;
            var form = HttpContext.Request.Form;

            var allIdsToAdd = form["addtocart"].FirstOrDefault() != null
                ? form["addtocart"].Select(x => int.Parse(x)).ToList()
                : new List<int>();

            foreach (var cartItem in pageCart.Items.Where(x => allIdsToAdd.Contains(x.Item.Id)))
            {
                var addToCartContext = new AddToCartContext
                {
                    Customer = Services.WorkContext.CurrentCustomer,
                    CartType = ShoppingCartType.ShoppingCart,
                    StoreId = storeId,
                    RawAttributes = cartItem.Item.RawAttributes,
                    ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
                    CustomerEnteredPrice = new(cartItem.Item.CustomerEnteredPrice, _currencyService.PrimaryCurrency),
                    Product = cartItem.Item.Product,
                    Quantity = cartItem.Item.Quantity
                };

                if (await _shoppingCartService.CopyAsync(addToCartContext))
                {
                    numberOfAddedItems++;
                }

                if (_shoppingCartSettings.MoveItemsFromWishlistToCart && !customerGuid.HasValue && addToCartContext.Warnings.Count == 0)
                {
                    await _shoppingCartService.DeleteCartItemAsync(cartItem.Item);
                }

                allWarnings.AddRange(addToCartContext.Warnings);
            }

            if (numberOfAddedItems > 0)
            {
                return RedirectToRoute("ShoppingCart");
            }

            var wishlist = await _shoppingCartService.GetCartAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);
            var model = new WishlistModel();
            await wishlist.MapAsync(model, !customerGuid.HasValue);

            NotifyInfo(T("Products.SelectProducts"), true);

            return View(model);
        }

        #endregion

        #region Email wishlist

        [RequireSsl]
        [GdprConsent]
        public async Task<IActionResult> EmailWishlist()
        {
            if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
                return RedirectToRoute("Homepage");

            var customer = Services.WorkContext.CurrentCustomer;
            var wishlist = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
            if (!wishlist.Items.Any())
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
        [ValidateCaptcha(CaptchaSettingName = nameof(CaptchaSettings.ShowOnEmailWishlistToFriendPage))]
        [GdprConsent]
        public async Task<IActionResult> EmailWishlistSend(WishlistEmailAFriendModel model, string captchaError)
        {
            if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
            {
                return RedirectToRoute("Homepage");
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var wishlist = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
            if (!wishlist.Items.Any())
            {
                return RedirectToRoute("Homepage");
            }

            if (_captchaSettings.ShowOnEmailWishlistToFriendPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            // Check whether the current customer is guest and is allowed to email wishlist.
            if (customer.IsGuest() && !_shoppingCartSettings.AllowAnonymousUsersToEmailWishlist)
            {
                ModelState.AddModelError(string.Empty, T("Wishlist.EmailAFriend.OnlyRegisteredUsers"));
            }

            if (!ModelState.IsValid)
            {
                // If we got this far, something failed, redisplay form.
                ModelState.AddModelError(string.Empty, T("Common.Error.Sendmail"));
                model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailWishlistToFriendPage;

                return View(model);
            }

            await _messageFactory.SendShareWishlistMessageAsync(
                customer,
                model.YourEmailAddress,
                model.FriendEmail,
                HtmlUtility.ConvertPlainTextToHtml(model.PersonalMessage.HtmlEncode()));

            model.SuccessfullySent = true;
            model.Result = T("Wishlist.EmailAFriend.SuccessfullySent");

            return View(model);
        }

        #endregion

        [HttpPost, ActionName("Cart")]
        [IgnoreAntiforgeryToken]
        [FormValueRequired("estimateshipping")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> GetEstimateShipping(EstimateShippingModel shippingModel, ProductVariantQuery query)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var currency = Services.WorkContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(storeId: storeId);

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
            await _db.SaveChangesAsync();

            var model = await cart.MapAsync(setEstimateShippingDefaultAddress: false);

            model.EstimateShipping.CountryId = shippingModel.CountryId;
            model.EstimateShipping.StateProvinceId = shippingModel.StateProvinceId;
            model.EstimateShipping.ZipPostalCode = shippingModel.ZipPostalCode;

            if (cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                var shippingInfoUrl = await Url.TopicAsync("ShippingInfo");
                if (shippingInfoUrl.HasValue())
                {
                    model.EstimateShipping.ShippingInfoUrl = shippingInfoUrl;
                }

                var address = new Address
                {
                    CountryId = shippingModel.CountryId,
                    Country = await _db.Countries.FindByIdAsync(shippingModel.CountryId.GetValueOrDefault(), false),
                    StateProvinceId = shippingModel.StateProvinceId,
                    StateProvince = await _db.StateProvinces.FindByIdAsync(shippingModel.StateProvinceId.GetValueOrDefault(), false),
                    ZipPostalCode = shippingModel.ZipPostalCode,
                };

                var getShippingOptionResponse = await _shippingService.GetShippingOptionsAsync(cart, address, storeId: storeId);
                if (!getShippingOptionResponse.Success)
                {
                    model.EstimateShipping.Warnings.AddRange(getShippingOptionResponse.Errors);
                }
                else
                {
                    if (getShippingOptionResponse.ShippingOptions.Any())
                    {
                        var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(storeId);
                        var shippingTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.ShippingCharge);

                        foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                        {
                            var soModel = new EstimateShippingModel.ShippingOptionModel
                            {
                                ShippingMethodId = shippingOption.ShippingMethodId,
                                Name = shippingOption.Name,
                                Description = shippingOption.Description
                            };

                            var (shippingAmount, _) = await _orderCalculationService.AdjustShippingRateAsync(cart, shippingOption.Rate, shippingOption, shippingMethods);
                            var rateBase = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount);
                            var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, currency);
                            soModel.Price = rate.WithPostFormat(shippingTaxFormat).ToString();

                            model.EstimateShipping.ShippingOptions.Add(soModel);
                        }
                    }
                    else
                    {
                        model.EstimateShipping.Warnings.Add(T("Checkout.ShippingIsNotAllowed"));
                    }
                }
            }

            return View(model);
        }

        #region Upload

        [HttpPost]
        [MaxMediaFileSize]
        public async Task<IActionResult> UploadFileProductAttribute(int productId, int productAttributeId)
        {
            var product = await _db.Products.FindByIdAsync(productId, false);
            if (product == null || !product.Published || product.Deleted || product.IsSystemProduct)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }

            // Ensure that this attribute belongs to this product and has the "file upload" type
            var variantAttribute = await _db.ProductVariantAttributes
                .AsNoTracking()
                .ApplyProductFilter(new[] { productId })
                .Include(x => x.ProductAttribute)
                .Where(x => x.ProductAttributeId == productAttributeId)
                .FirstOrDefaultAsync();

            if (variantAttribute == null || variantAttribute.AttributeControlType != AttributeControlType.FileUpload)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                });
            }

            var postedFile = Request.Form.Files.FirstOrDefault();
            if (postedFile == null)
            {
                throw new ArgumentException(T("Common.NoFileUploaded"));
            }

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = string.Empty,
                UpdatedOnUtc = DateTime.UtcNow,
                EntityId = productId,
                EntityName = "ProductAttribute",
                IsTransient = true
            };

            using var stream = postedFile.OpenReadStream();
            var mediaFile = await _downloadService.InsertDownloadAsync(download, stream, postedFile.FileName);

            return Json(new
            {
                id = download.MediaFileId,
                name = mediaFile.Name,
                type = mediaFile.MediaType,
                thumbUrl = _mediaService.GetUrl(download.MediaFile, _mediaSettings.ProductThumbPictureSize, string.Empty),
                success = true,
                message = T("ShoppingCart.FileUploaded").Value,
                downloadGuid = download.DownloadGuid,
            });
        }

        [HttpPost]
        [MaxMediaFileSize]
        public async Task<IActionResult> UploadFileCheckoutAttribute()
        {
            var fileResult = Request.Form.Files.FirstOrDefault();
            if (fileResult == null || !fileResult.FileName.HasValue())
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty
                });
            }

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = string.Empty,
                UpdatedOnUtc = DateTime.UtcNow,
                EntityId = 0,
                EntityName = "CheckoutAttribute",
                IsTransient = true
            };

            using var stream = fileResult.OpenReadStream();
            var mediaFile = await _downloadService.InsertDownloadAsync(download, stream, fileResult.FileName);

            return Json(new
            {
                id = download.MediaFileId,
                name = mediaFile.Name,
                type = mediaFile.MediaType,
                thumbUrl = await _mediaService.GetUrlAsync(mediaFile.File.Id, _mediaSettings.ProductThumbPictureSize, host: string.Empty),
                success = true,
                message = T("ShoppingCart.FileUploaded").Value,
                downloadGuid = download.DownloadGuid,
            });
        }

        #endregion

        #region Discount/GiftCard coupon codes & Reward points

        /// <summary>
        /// Tries to apply <paramref name="discountCouponcode"/> as <see cref="Discount"/> and applies 
        /// selected checkout attributes.
        /// </summary>
        /// <param name="query">The <see cref="ProductVariantQuery"/></param>
        /// <param name="discountCouponcode">The <see cref="Discount.CouponCode"/> to apply.</param>
        /// <returns></returns>
        [HttpPost, ActionName("Cart")]
        [FormValueRequired("applydiscountcouponcode")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> ApplyDiscountCoupon(ProductVariantQuery query, string discountCouponcode)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var model = await cart.MapAsync();

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

            model.DiscountBox.IsWarning = true;

            if (discountCouponcode.HasValue())
            {
                var discount = await _db.Discounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CouponCode == discountCouponcode);

                var isDiscountValid = discount != null
                    && discount.RequiresCouponCode
                    && await _discountService.IsDiscountValidAsync(discount, cart.Customer, discountCouponcode);

                if (isDiscountValid)
                {
                    var discountApplied = true;
                    var oldCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

                    cart.Customer.GenericAttributes.DiscountCouponCode = discountCouponcode;

                    if (oldCartTotal.Total.HasValue)
                    {
                        var newCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
                        discountApplied = oldCartTotal.Total != newCartTotal.Total;
                    }

                    if (discountApplied)
                    {
                        model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.Applied");
                        model.DiscountBox.IsWarning = false;
                    }
                    else
                    {
                        model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.NoMoreDiscount");

                        cart.Customer.GenericAttributes.DiscountCouponCode = null;
                    }
                }
                else
                {
                    model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
                }
            }
            else
            {
                model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
            }

            await _db.SaveChangesAsync();

            return View(model);
        }

        /// <summary>
        /// Removes the applied discount coupon code from current customer.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> RemoveDiscountCoupon()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);

            var model = await cart.MapAsync();

            customer.GenericAttributes.DiscountCouponCode = null;

            var discountHtml = await InvokePartialViewAsync("_DiscountBox", model.DiscountBox);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            // Updated cart.
            return Json(new
            {
                success = true,
                totalsHtml,
                discountHtml,
                displayCheckoutButtons = true
            });
        }

        /// <summary>
        /// Applies gift card by coupon code to cart.
        /// </summary>
        /// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        /// <param name="giftCardCouponCode">The <see cref="GiftCard.GiftCardCouponCode"/> to apply.</param>
        [HttpPost, ActionName("Cart")]
        [FormValueRequired("applygiftcardcouponcode")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> ApplyGiftCard(ProductVariantQuery query, string giftCardCouponCode)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var model = await cart.MapAsync();
            model.GiftCardBox.IsWarning = true;

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                if (giftCardCouponCode.HasValue())
                {
                    var giftCard = await _db.GiftCards
                        .Include(x => x.GiftCardUsageHistory)
                        .AsNoTracking()
                        .ApplyCouponFilter(new[] { giftCardCouponCode })
                        .FirstOrDefaultAsync();

                    var isGiftCardValid = giftCard != null && await _giftCardService.ValidateGiftCardAsync(giftCard, cart.StoreId);
                    if (isGiftCardValid)
                    {
                        var couponCodes = new List<GiftCardCouponCode>(cart.Customer.GenericAttributes.GiftCardCouponCodes);
                        if (!couponCodes.Select(x => x.Value).Contains(giftCardCouponCode))
                        {
                            couponCodes.Add(new GiftCardCouponCode(giftCardCouponCode));

                            cart.Customer.GenericAttributes.GiftCardCouponCodes = couponCodes;
                        }

                        model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.Applied");
                        model.GiftCardBox.IsWarning = false;
                    }
                    else
                    {
                        model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
                    }
                }
                else
                {
                    model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
                }
            }
            else
            {
                model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.DontWorkWithAutoshipProducts");
            }

            await _db.SaveChangesAsync();

            return View(model);
        }

        /// <summary>
        /// Removes applied gift card by <paramref name="giftCardId"/> from customer.
        /// </summary>
        /// <param name="giftCardId"><see cref="GiftCard"/> identifier to remove.</param>        
        [HttpPost]
        public async Task<IActionResult> RemoveGiftCardCode(int giftCardId)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: storeId);
            var model = await cart.MapAsync();

            var giftCard = await _db.GiftCards.FindByIdAsync(giftCardId, false);
            if (giftCard != null)
            {
                var giftCards = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
                var found = giftCards.Where(x => x.Value == giftCard.GiftCardCouponCode).FirstOrDefault();
                if (giftCards.Remove(found))
                {
                    customer.GenericAttributes.GiftCardCouponCodes = giftCards;
                }
            }

            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            // Updated cart.
            return Json(new
            {
                success = true,
                totalsHtml,
                displayCheckoutButtons = true
            });
        }

        [HttpPost, ActionName("Cart")]
        [FormValueRequired("applyrewardpoints")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> ApplyRewardPoints(ProductVariantQuery query, bool useRewardPoints = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var model = new ShoppingCartModel();
            await cart.MapAsync(model);

            model.RewardPoints.UseRewardPoints = useRewardPoints;

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
            cart.Customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;

            await _db.SaveChangesAsync();

            return View(model);
        }

        #endregion
    }
}