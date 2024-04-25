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
using Smartstore.Core.Seo.Routing;
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
            var cartEnabled = cart && _shoppingCartSettings.MiniShoppingCartEnabled && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart, customer);
            var wishlistEnabled = wishlist && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist, customer);
            var compareEnabled = compare && _catalogSettings.CompareProductsEnabled;

            return Json(new
            {
                CartItemsCount = cartEnabled ? await _shoppingCartService.CountProductsInCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id) : 0,
                WishlistItemsCount = wishlistEnabled ? await _shoppingCartService.CountProductsInCartAsync(customer, ShoppingCartType.Wishlist, store.Id) : 0,
                CompareItemsCount = compareEnabled ? await _productCompareService.CountComparedProductsAsync() : 0
            });
        }

        [DisallowRobot(true)]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return RedirectToRoute("Homepage");
            }

            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id, activeOnly: null);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Count > 0)
            {
                cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
                await _db.SaveChangesAsync();
            }

            var validateCheckoutAttributes = (TempData["ValidateCheckoutAttributes"] as bool?) ?? false;
            var model = await cart.MapAsync(validateCheckoutAttributes: validateCheckoutAttributes);

            ViewBag.CartItemSelectionLink = GetCartItemSelectionLink(cart);

            return View(model);
        }

        [DisallowRobot]
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
        /// Validates and saves cart data. Customer is redirected to the checkout process if the cart is valid,
        /// otherwise he is redirected back to the shopping cart.
        /// </summary>
        [HttpPost, ActionName("Cart")]
        [FormValueRequired("startcheckout")]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> StartCheckout(ProductVariantQuery query, bool useRewardPoints = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var warnings = new List<string>();

            // Save data entered on cart page.
            if (!await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false))
            {
                TempData["ValidateCheckoutAttributes"] = true;

                return RedirectToRoute("ShoppingCart");
            }

            if (cart.Customer.IsGuest())
            {
                if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                {
                    return RedirectToAction(nameof(CheckoutController.BillingAddress), "Checkout");
                }

                return RedirectToRoute("Login", new { checkoutAsGuest = _orderSettings.AnonymousCheckoutAllowed, returnUrl = Url.RouteUrl("ShoppingCart") });
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
            if (!_shoppingCartSettings.MiniShoppingCartEnabled
                || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return new EmptyResult();
            }

            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
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
            await wishlist.MapAsync(model, isOffcanvas: true);

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
        [DisallowRobot]
        public async Task<IActionResult> SaveCartData(ProductVariantQuery query, bool? useRewardPoints)
        {
            var warnings = new List<string>();
            var success = await _shoppingCartService.SaveCartDataAsync(null, warnings, query, useRewardPoints);

            return Json(new { success, warnings });
        }

        /// <summary>
        /// AJAX. Updates cart item of a shopping cart (e.g. the item quantity).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateCartItem(UpdateCartItemModel model)
        {
            return await UpdateCartItemInternal(model, false);
        }

        /// <summary>
        /// AJAX. Removes a cart item from either the shopping cart or the wishlist.
        /// </summary>
        [HttpPost]
        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> DeleteCartItem(UpdateCartItemModel model)
        {
            return await UpdateCartItemInternal(model, true);
        }

        private async Task<IActionResult> UpdateCartItemInternal(UpdateCartItemModel model, bool delete)
        {
            var permission = model.IsWishlist ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart;
            if (!await Services.Permissions.AuthorizeAsync(permission))
            {
                return Json(new
                {
                    success = false,
                    message = await Services.Permissions.GetUnauthorizedMessageAsync(permission)
                });
            }

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var cartType = model.IsWishlist ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart;
            var cartHtml = string.Empty;
            var totalsHtml = string.Empty;
            var warningsHtml = string.Empty;
            var itemSelectionHtml = string.Empty;
            var newItemPrice = string.Empty;
            var message = string.Empty;
            var subtotal = Money.Zero;
            var success = true;

            if (delete)
            {
                var item = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == model.CartItemId);
                if (item == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = T("ShoppingCart.DeleteCartItem.Failed").Value
                    });
                }

                await _shoppingCartService.DeleteCartItemAsync(item, true, true);
                message = T("ShoppingCart.DeleteCartItem.Success");
            }
            else if (model.ActivateAll.HasValue)
            {
                customer.ShoppingCartItems
                    .FilterByCartType(cartType, store.Id, null, false)
                    .Each(x => x.Active = model.ActivateAll.Value);
            }
            else
            {
                var warnings = await _shoppingCartService.UpdateCartItemAsync(customer, model.CartItemId, model.NewQuantity, model.Active);
                message = string.Join(". ", warnings.Take(3));
                success = warnings.Count == 0;
            }

            var cart = await _shoppingCartService.GetCartAsync(customer, cartType, store.Id, null);

            if (model.IsCartPage || delete)
            {
                if (model.IsWishlist)
                {
                    var wishlistModel = new WishlistModel();
                    await cart.MapAsync(wishlistModel);

                    cartHtml = await InvokePartialViewAsync("WishlistItems", wishlistModel);
                    warningsHtml = await InvokePartialViewAsync("CartWarnings", wishlistModel.Warnings);
                }
                else
                {
                    var cartModel = await cart.MapAsync();
                    var item = cartModel.Items.FirstOrDefault(x => x.Id == model.CartItemId);

                    cartHtml = await InvokePartialViewAsync("CartItems", cartModel);
                    totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });
                    itemSelectionHtml = GetCartItemSelectionLink(cart);
                    warningsHtml = await InvokePartialViewAsync("CartWarnings", cartModel.Warnings);

                    if (item != null)
                    {
                        newItemPrice = item.Price.UnitPrice.ToString();
                    }
                }
            }

            if (!delete)
            {
                var currency = Services.WorkContext.WorkingCurrency;
                var cartSubtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, activeOnly: true);
                var subtotalWithoutDiscount = _currencyService.ConvertFromPrimaryCurrency(cartSubtotal.SubtotalWithoutDiscount.Amount, currency);

                subtotal = subtotalWithoutDiscount.WithPostFormat(_taxService.GetTaxFormat());
            }

            return Json(new
            {
                success,
                SubTotal = subtotal,
                SubTotalValue = subtotal.Amount,
                newItemPrice,
                checkoutAllowed = cart.Items.Any(x => x.Active),
                cartItemCount = cart.Items.Length,
                message,
                cartHtml,
                totalsHtml,
                itemSelectionHtml,
                warningsHtml
            });
        }

        /// <summary>
        /// Adds a product without variants to the cart or redirects user to product details page.
        /// This method is used in product lists on catalog pages (category/manufacturer etc...).
        /// </summary>
        /// <param name="productId">Identifier of the <see cref="Product"/> to add.</param>
        /// <param name="shoppingCartTypeId"><see cref="ShoppingCartType"/> value.</param>
        /// <param name="forceRedirection">A value indicating whether to force a redirection to the shopping cart.</param>
        [HttpPost]
        [DisallowRobot]
        [IgnoreAntiforgeryToken]
        [SaveChanges<SmartDbContext>(false)]
        [LocalizedRoute("/cart/addproductsimple/{productId:int}", Name = "AddProductToCartSimple")]
        public async Task<IActionResult> AddProductSimple(int productId, int shoppingCartTypeId = 1, bool forceRedirection = false)
        {
            var product = await _db.Products.FindByIdAsync(productId);
            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    productId,
                    message = T("Products.NotFound", productId)
                });
            }

            // Filter out cases where a product cannot be added to the cart.
            if (product.ProductType == ProductType.GroupedProduct || product.CustomerEntersPrice || product.IsGiftCard)
            {
                return await RedirectToProduct();
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            if (allowedQuantities.Length > 0)
            {
                // The user must select a quantity from the dropdown list, therefore the product cannot be added to the cart.
                return await RedirectToProduct();
            }

            // Product looks good so far, let's try adding the product to the cart (with product attribute validation etc.).
            var addToCartContext = new AddToCartContext
            {
                Product = product,
                CartType = (ShoppingCartType)shoppingCartTypeId,
                Quantity = product.GetMinOrderQuantity(),
                AutomaticallyAddRequiredProducts = product.RequireOtherProducts && product.AutomaticallyAddRequiredProducts,
                AutomaticallyAddBundleProducts = true
            };

            if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
            {
                // Item could not be added to the cart. Most likely, the customer has to select something on the product detail page e.g. variant attributes, giftcard infos, etc..
                return await RedirectToProduct();
            }

            // Product has been added to the cart. Add to activity log.
            _activityLogger.LogActivity(KnownActivityLogTypes.PublicStoreAddToShoppingCart, T("ActivityLog.PublicStore.AddToShoppingCart"), product.Name);

            if (_shoppingCartSettings.DisplayCartAfterAddingProduct || forceRedirection)
            {
                // Redirect to the shopping cart page.
                return Json(new
                {
                    productId,
                    redirect = Url.RouteUrl("ShoppingCart"),
                });
            }

            return Json(new
            {
                success = true,
                productId,
                message = T("Products.ProductHasBeenAddedToTheCart", Url.RouteUrl("ShoppingCart")).Value
            });

            async Task<JsonResult> RedirectToProduct()
            {
                return Json(new
                {
                    productId,
                    redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
                });
            }
        }

        /// <summary>
        /// Adds a product to the cart from the product details page.
        /// </summary>
        /// <param name="productId">Identifier of the <see cref="Product"/> to add.</param>
        /// <param name="shoppingCartTypeId"><see cref="ShoppingCartType"/> value.</param>
        /// <param name="query">The <see cref="ProductVariantQuery"/> of selected attributes.</param>
        [HttpPost]
        [DisallowRobot]
        [IgnoreAntiforgeryToken]
        [SaveChanges<SmartDbContext>(false)]
        [LocalizedRoute("/cart/addproduct/{productId:int}/{shoppingCartTypeId:int}", Name = "AddProductToCart")]
        public async Task<IActionResult> AddProduct(int productId, int shoppingCartTypeId, ProductVariantQuery query)
        {
            var product = await _db.Products
                .Include(x => x.ProductVariantAttributes)
                .FindByIdAsync(productId);
            if (product == null)
            {
                return Json(new
                { 
                    productId, 
                    redirect = Url.RouteUrl("Homepage")
                });
            }

            var form = HttpContext.Request.Form;
            var enteredPriceConverted = new Money();

            if (product.CustomerEntersPrice)
            {
                foreach (var formKey in form.Keys)
                {
                    if (formKey.EqualsNoCase($"addtocart_{productId}.CustomerEnteredPrice"))
                    {
                        if (ConvertUtility.TryConvert<decimal>(form[formKey].First(), out var enteredPrice))
                        {
                            enteredPriceConverted = _currencyService.ConvertToPrimaryCurrency(new(enteredPrice, Services.WorkContext.WorkingCurrency));
                        }
                        break;
                    }
                }
            }

            var quantity = product.GetMinOrderQuantity();
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

            var addToCartContext = new AddToCartContext
            {
                Product = product,
                VariantQuery = query,
                CartType = (ShoppingCartType)shoppingCartTypeId,
                CustomerEnteredPrice = enteredPriceConverted,
                Quantity = quantity,
                AutomaticallyAddRequiredProducts = product.RequireOtherProducts && product.AutomaticallyAddRequiredProducts,
                AutomaticallyAddBundleProducts = true
            };

            if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
            {
                // Product could not be added to the cart/wishlist.
                return Json(new
                {
                    success = false,
                    productId,
                    message = addToCartContext.Warnings.ToArray()
                });
            }

            // Product was successfully added to the cart/wishlist.
            bool redirect;
            string routeUrl, activity, resourceName;

            switch (addToCartContext.CartType)
            {
                case ShoppingCartType.Wishlist:
                    redirect = _shoppingCartSettings.DisplayWishlistAfterAddingProduct;
                    routeUrl = "Wishlist";
                    activity = KnownActivityLogTypes.PublicStoreAddToWishlist;
                    resourceName = "ActivityLog.PublicStore.AddToWishlist";
                    break;
                case ShoppingCartType.ShoppingCart:
                default:
                    redirect = _shoppingCartSettings.DisplayCartAfterAddingProduct;
                    routeUrl = "ShoppingCart";
                    activity = KnownActivityLogTypes.PublicStoreAddToShoppingCart;
                    resourceName = "ActivityLog.PublicStore.AddToShoppingCart";
                    break;
            }

            _activityLogger.LogActivity(activity, T(resourceName), product.Name);

            if (redirect)
            {
                return Json(new { productId, redirect = Url.RouteUrl(routeUrl) });
            }

            return Json(new { success = true, productId });
        }

        /// <summary>
        /// Moves item from either Wishlist to ShoppingCart or vice versa.
        /// </summary>
        /// <param name="cartItemId">The identifier of <see cref="OrganizedShoppingCartItem"/>.</param>
        /// <param name="cartType">The <see cref="ShoppingCartType"/> from which to move the item from.</param>
        /// <param name="isCartPage">A value indicating whether the user is on cart page (prepares model).</param>        
        [HttpPost, ActionName("MoveItemBetweenCartAndWishlist")]
        [DisallowRobot]
        [IgnoreAntiforgeryToken]
        [SaveChanges<SmartDbContext>(false)]
        public async Task<IActionResult> MoveItemBetweenCartAndWishlistAjax(int cartItemId, ShoppingCartType cartType, bool isCartPage = false)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart) ||
                !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
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
                cartItemCount
            });
        }

        [HttpPost, ActionName("Wishlist")]
        [DisallowRobot]
        [FormValueRequired("addtocartbutton")]
        [LocalizedRoute("/wishlist/{customerGuid:guid?}", Name = "Wishlist")]
        public async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart) ||
                !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
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
                ? form["addtocart"].Select(int.Parse).ToList()
                : [];

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

        [GdprConsent]
        public async Task<IActionResult> EmailWishlist()
        {
            if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
            {
                return RedirectToRoute("Homepage");
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var wishlist = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
            if (wishlist.Items.Length == 0)
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
            if (wishlist.Items.Length == 0)
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
                .ApplyProductFilter([productId])
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

            var postedFile = Request.Form.Files.FirstOrDefault() ?? throw new ArgumentException(T("Common.NoFileUploaded"));

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

        #region Discount coupon code

        [HttpPost]
        public async Task<IActionResult> ApplyDiscountCoupon(ProductVariantQuery query, string discountCouponCode)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

            var (applied, discount) = await _orderCalculationService.ApplyDiscountCouponAsync(cart, discountCouponCode);
            await _db.SaveChangesAsync();

            var model = await cart.MapAsync();
            model.DiscountBox.IsWarning = !applied;
            model.DiscountBox.Message = applied
                ? T("ShoppingCart.DiscountCouponCode.Applied")
                : T(discount == null ? "ShoppingCart.DiscountCouponCode.WrongDiscount" : "ShoppingCart.DiscountCouponCode.NoMoreDiscount");

            var discountHtml = await InvokePartialViewAsync("_DiscountBox", model.DiscountBox);
            var cartHtml = await InvokePartialViewAsync("CartItems", model);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            // Always "success = true" to render discountHtml.
            return Json(new
            {
                success = true,
                cartHtml,
                totalsHtml,
                discountHtml
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveDiscountCoupon()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            customer.GenericAttributes.DiscountCouponCode = null;
            await _db.SaveChangesAsync();

            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);
            var model = await cart.MapAsync();

            var discountHtml = await InvokePartialViewAsync("_DiscountBox", model.DiscountBox);
            var cartHtml = await InvokePartialViewAsync("CartItems", model);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            return Json(new
            {
                success = true,
                cartHtml,
                totalsHtml,
                discountHtml
            });
        }

        #endregion

        #region Giftcard coupon code

        [HttpPost]
        public async Task<IActionResult> ApplyGiftCardCoupon(ProductVariantQuery query, string giftCardCouponCode)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);

            string message = null;
            var success = false;

            if (!cart.IncludesMatchingItems(x => x.IsRecurring))
            {
                if (giftCardCouponCode.HasValue())
                {
                    var giftCard = await _db.GiftCards
                        .Include(x => x.GiftCardUsageHistory)
                        .AsNoTracking()
                        .ApplyCouponFilter([giftCardCouponCode])
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

                        success = true;
                        message = T("ShoppingCart.GiftCardCouponCode.Applied");
                    }
                    else
                    {
                        message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
                    }
                }
                else
                {
                    message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
                }
            }
            else
            {
                message = T("ShoppingCart.GiftCardCouponCode.DontWorkWithAutoshipProducts");
            }

            await _db.SaveChangesAsync();

            var model = await cart.MapAsync();
            model.GiftCardBox.Message = message;
            model.GiftCardBox.IsWarning = !success;

            var giftCardHtml = await InvokePartialViewAsync("_GiftCardBox", model.GiftCardBox);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            return Json(new
            {
                success = true,
                totalsHtml,
                giftCardHtml
            });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveGiftCardCoupon(int giftCardId)
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var cart = await _shoppingCartService.GetCartAsync(customer, storeId: Services.StoreContext.CurrentStore.Id);

            var giftCard = await _db.GiftCards.FindByIdAsync(giftCardId, false);
            if (giftCard != null)
            {
                var giftCards = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
                var foundGiftCard = giftCards.FirstOrDefault(x => x.Value == giftCard.GiftCardCouponCode);

                if (giftCards.Remove(foundGiftCard))
                {
                    customer.GenericAttributes.GiftCardCouponCodes = giftCards;
                    await _db.SaveChangesAsync();
                }
            }

            var model = await cart.MapAsync();

            var giftCardHtml = await InvokePartialViewAsync("_GiftCardBox", model.GiftCardBox);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            return Json(new
            {
                success = true,
                totalsHtml,
                giftCardHtml
            });
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> ApplyRewardPoints(ProductVariantQuery query, bool useRewardPoints = false)
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
            cart.Customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;

            await _db.SaveChangesAsync();

            var model = await cart.MapAsync();
            model.RewardPoints.UseRewardPoints = useRewardPoints;

            var rewardPointsHtml = await InvokePartialViewAsync("_RewardPointsBox", model.RewardPoints);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            return Json(new
            {
                success = true,
                totalsHtml,
                rewardPointsHtml
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EstimateShipping(ProductVariantQuery query, EstimateShippingModel shippingModel)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var cart = await _shoppingCartService.GetCartAsync(storeId: storeId);

            cart.Customer.GenericAttributes.CheckoutAttributes = await _checkoutAttributeMaterializer.CreateCheckoutAttributeSelectionAsync(query, cart);
            await _db.SaveChangesAsync();

            var model = await cart.MapAsync(setEstimateShippingDefaultAddress: false);

            model.EstimateShipping.CountryId = shippingModel.CountryId;
            model.EstimateShipping.StateProvinceId = shippingModel.StateProvinceId;
            model.EstimateShipping.ZipPostalCode = shippingModel.ZipPostalCode;

            if (cart.IsShippingRequired)
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

                var (options, warnings) = await GetEstimatedShippingOptions(cart, address, true);
                if (options.Count == 0 && warnings.Count > 0)
                {
                    (options, warnings) = await GetEstimatedShippingOptions(cart, address, false);
                }

                model.EstimateShipping.ShippingOptions.AddRange(options);
                model.EstimateShipping.Warnings.AddRange(warnings);
            }

            var estimateShippingHtml = await InvokePartialViewAsync("EstimateShipping", model.EstimateShipping);
            var totalsHtml = await InvokeComponentAsync(typeof(OrderTotalsViewComponent), ViewData, new { isEditable = true });

            return Json(new
            {
                success = true,
                totalsHtml,
                estimateShippingHtml
            });
        }

        private async Task<(List<EstimateShippingModel.ShippingOptionModel> Options, List<string> Warnings)> GetEstimatedShippingOptions(
            ShoppingCart cart,
            Address address,
            bool matchRules)
        {
            var options = new List<EstimateShippingModel.ShippingOptionModel>();
            var optionResponse = await _shippingService.GetShippingOptionsAsync(cart, address, null, cart.StoreId, matchRules);

            if (!optionResponse.Success)
            {
                return (options, optionResponse.Errors);
            }

            if (optionResponse.ShippingOptions.Count == 0)
            {
                return (options, [T("Checkout.ShippingIsNotAllowed")]);
            }

            var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(cart.StoreId, matchRules);
            var shippingTaxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.ShippingCharge);
            var currency = Services.WorkContext.WorkingCurrency;

            foreach (var shippingOption in optionResponse.ShippingOptions)
            {
                var (shippingAmount, _) = await _orderCalculationService.AdjustShippingRateAsync(cart, shippingOption.Rate, shippingOption, shippingMethods);
                var rateBase = await _taxCalculator.CalculateShippingTaxAsync(shippingAmount);
                var rate = _currencyService.ConvertFromPrimaryCurrency(rateBase.Price, currency);

                options.Add(new()
                {
                    ShippingMethodId = shippingOption.ShippingMethodId,
                    Name = shippingOption.Name,
                    Description = shippingOption.Description,
                    Price = rate.WithPostFormat(shippingTaxFormat).ToString()
                });
            }

            return (options, []);
        }

        private string GetCartItemSelectionLink(ShoppingCart cart)
        {
            if (_shoppingCartSettings.AllowActivatableCartItems && cart.HasItems)
            {
                var activateAll = true;
                string resKey = null;

                if (cart.Items.All(x => x.Active))
                {
                    activateAll = false;
                    resKey = "ShoppingCart.DeselectAllProducts";
                }
                else if (!cart.Items.Any(x => x.Active))
                {
                    resKey = "ShoppingCart.NoProductsSelectedSelectAll";
                }
                else
                {
                    resKey = "ShoppingCart.SelectAllProducts";
                }

                return T(resKey, Url.Action(nameof(UpdateCartItem), "ShoppingCart", new { activateAll }));
            }

            return string.Empty;
        }
    }
}