using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Controllers
{
    // TODO: (mh) (core) Consolidate both controllers into one? TBD with MC.
    public class PayPalController : PublicController
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IShoppingCartService _shoppingCartService;

        public PayPalController(ICheckoutStateAccessor checkoutStateAccessor, IShoppingCartService shoppingCartService)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartService = shoppingCartService;
        }

        [HttpPost]
        public async Task<IActionResult> InitTransaction(ProductVariantQuery query, bool? useRewardPoints, string orderId)
        {
            var success = false;
            var message = string.Empty;

            if (!orderId.HasValue())
            {
                return Json(new { success, message = "No order id has been returned by PayPal." });
            }

            // Save data entered on cart page & validate cart and return warnings for minibasket.
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var warnings = new List<string>();
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints);
            if (isCartValid)
            {
                var checkoutState = _checkoutStateAccessor.CheckoutState;

                // Set flag which indicates to skip payment selection.
                checkoutState.CustomProperties["PayPalButtonUsed"] = true;

                // Store order id temporarily in checkout state.
                checkoutState.CustomProperties["PayPalOrderId"] = orderId;

                success = true;
            }
            else
            {
                message = string.Join(Environment.NewLine, warnings);
            }

            // TODO: (mh) (core) Write id to order once order is available & delete afterwards.
            // Also write authorization id or 

            return Json(new { success, message });
        }
    }
}