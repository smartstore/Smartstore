using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Controllers
{
    // TODO: (mh) (core) Consolidate both controllers into one? TBD with MC.
    public class PayPalController : PublicController
    {
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        
        public PayPalController(ICheckoutStateAccessor checkoutStateAccessor)
        {
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        [HttpPost]
        public IActionResult InitTransaction(string orderId)
        {
            var success = false;
            
            if (!orderId.HasValue())
            {
                return Json(new { success, message = "No order id has been returned by PayPal." });
            }

            var checkoutState = _checkoutStateAccessor.CheckoutState;

            // Set flag which indicates to skip payment selection.
            checkoutState.CustomProperties["PayPalButtonUsed"] = true;

            // Store order id temporarily in checkout state.
            checkoutState.CustomProperties["PayPalOrderId"] = orderId;

            // TODO: (mh) (core) Write id to order once order is available & delete afterwards.
            
            success = true;

            return Json(new { success });
        }
    }
}
