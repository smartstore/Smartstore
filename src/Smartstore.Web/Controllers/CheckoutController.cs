using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Localization.Routing;
using Smartstore.Web.Models.Checkout;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.ShoppingCart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Controllers
{
    public class CheckoutController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly OrderSettings _orderSettings;

        public CheckoutController(
            SmartDbContext db,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            OrderSettings orderSettings)
        {
            _db = db;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _orderSettings = orderSettings;
        }

        [NonAction]
        protected async Task<CheckoutAddressModel> PrepareCheckoutAddressModelAsync(bool shipping = false, int? selectedCountryId = null)
        {
            // Get existing addresses.
            var customer = Services.WorkContext.CurrentCustomer;
            var addresses = new List<Address>(customer.Addresses).AsEnumerable();

            // And map billing / shipping addresses.
            var model = new CheckoutAddressModel();
            await addresses.MapAsync(model, shipping, selectedCountryId);

            return model;
        }

        [LocalizedRoute("/checkout", Name = "Checkout")]
        public async Task<IActionResult> Index()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var customer = Services.WorkContext.CurrentCustomer;

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            customer.ResetCheckoutData(storeId);

            // Validate checkout attributes.
            var checkoutAttributes = customer.GenericAttributes.CheckoutAttributes;

            var warnings = new List<string>();
            var isValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings, true, checkoutAttributes);
            if (!isValid)
            {
                NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                return RedirectToRoute("ShoppingCart");
            }

            var validatingCartEvent = new ValidatingCartEvent(cart, warnings, customer);
            await Services.EventPublisher.PublishAsync(validatingCartEvent);

            if (validatingCartEvent.Result != null)
            {
                return validatingCartEvent.Result;
            }

            if (warnings.Any())
            {
                NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                return RedirectToRoute("ShoppingCart");
            }

            // Validate each shopping cart item.
            foreach (var cartItem in cart)
            {
                var ctx = new AddToCartContext
                {
                    StoreId = storeId,
                    Product = cartItem.Item.Product,
                    BundleItem = cartItem.Item.BundleItem,
                    ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList()
                };

                isValid = await _shoppingCartValidator.ValidateAddToCartItemAsync(ctx, cartItem.Item, cart);
                if (!isValid)
                {
                    warnings.AddRange(ctx.Warnings);
                    NotifyWarning(string.Join(Environment.NewLine, warnings.Take(3)));
                    return RedirectToRoute("ShoppingCart");
                }
            }

            return RedirectToAction("BillingAddress");
        }

        public async Task<IActionResult> BillingAddress()
        {
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            if (cart.Count == 0)
            {
                return RedirectToRoute("ShoppingCart");
            }

            if (Services.WorkContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            var model = await PrepareCheckoutAddressModelAsync(false);
            return View(model);
        }

        public async Task<IActionResult> ShippingAddress()
        {
            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            if (!cart.Any())
            {
                return RedirectToRoute("ShoppingCart");
            }

            var customer = Services.WorkContext.CurrentCustomer;

            if (customer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
            {
                return new UnauthorizedResult();
            }

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                customer.ShippingAddress = null;
                
                _db.Customers.Update(customer);
                await _db.SaveChangesAsync();

                return RedirectToAction("ShippingMethod");
            }

            var model = await PrepareCheckoutAddressModelAsync(true);
            return View(model);
        }
    }
}