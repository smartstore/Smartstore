using Microsoft.AspNetCore.Mvc;
using Smartstore.Checkout.Payment;
using Smartstore.Core.Logging;
using Smartstore.Klarna.Client; // For KlarnaHttpClient if direct API calls are needed here
using Smartstore.Web.Common.Controllers;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Orders; // Required for IOrderService
using Smartstore.Core.Data; // Required for SmartDbContext
using Smartstore.Web; // For FromBodyJsonAttribute if used for notifications
using System; // Required for Guid


namespace Smartstore.Klarna.Controllers
{
    // No Area attribute needed if it's a public controller for callbacks
    [Route("klarna/[action]")]
    public class KlarnaController : PublicController
    {
        private readonly ILogger _logger;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly SmartDbContext _db;

        public KlarnaController(
            ILogger logger,
            IPaymentService paymentService,
            IOrderService orderService,
            SmartDbContext db)
        {
            _logger = logger;
            _paymentService = paymentService;
            _orderService = orderService;
            _db = db;
        }

        // Example: Klarna Confirmation URL endpoint
        // Klarna redirects the user here after payment is authorized on Klarna's side.
        // The URL for this would be configured in KlarnaMerchantUrls.Confirmation
        [HttpGet]
        public async Task<IActionResult> Confirmation(string orderGuid, string klarnaOrderId, string sid /* Klarna session_id */)
        {
            // TODO: Validate parameters, retrieve order by orderGuid
            // Retrieve the authorization_token (this might be passed by Klarna, or you get it from the session `sid`)
            // Potentially, you'd read the Klarna session using `sid` to get the `authorization_token` if not directly provided.
            // Then, process the payment (which internally calls Capture if auto-capture is not used or if it's a two-step process)

            if (!Guid.TryParse(orderGuid, out var orderGuidValue))
            {
                _logger.Error("Klarna Confirmation: Invalid Order GUID received.");
                NotifyError("Invalid order identifier.");
                return RedirectToRoute("Homepage");
            }

            var order = await _orderService.GetOrderByGuidAsync(orderGuidValue);
            if (order == null)
            {
                _logger.Error($"Klarna Confirmation: Order with GUID {orderGuidValue} not found.");
                NotifyError("Order not found.");
                return RedirectToRoute("Homepage");
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
               return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }

            // At this point, the client-side Klarna widget should have authorized the payment.
            // The Klarna JS SDK typically provides an `authorization_token`.
            // This token must be sent from the client (e.g., via an AJAX call when Klarna's `authorize` callback fires)
            // to a dedicated server endpoint. That endpoint would then store this token, perhaps in `order.AuthorizationTransactionId`
            // or in `processPaymentRequest.CustomProperties` before calling `_paymentService.CaptureAsync`.

            // The 'Confirmation' endpoint here is usually just for the user's browser redirection.
            // The actual order capture should ideally happen via a server-to-server call triggered by the Klarna JS SDK callback.

            // If `sid` (Klarna Session ID) is available, and you have previously stored the `authorization_token`
            // against this session ID (e.g. in a temporary cache or in order custom properties), you could retrieve it.
            // However, relying on this for capture is less robust than the JS SDK sending it directly.

            _logger.Information($"Klarna Confirmation for Order GUID {orderGuid}, Klarna Order ID {klarnaOrderId}, Session ID {sid}. Order total: {order.OrderTotal}");

            // Assuming the authorization token was obtained and stored by another mechanism (e.g., AJAX call from client after Klarna widget authorization)
            // and CaptureAsync is the next step.
            // For many Klarna integrations, ProcessPaymentAsync (which creates the Klarna session) is called,
            // then client-side handles widget and authorization, then client tells server "hey I'm authorized, here's the token",
            // then server calls CaptureAsync with that token.

            // If this "Confirmation" URL is where Klarna expects the finalization:
            // 1. Ensure `order.AuthorizationTransactionId` is populated with the `authorization_token`.
            //    This might have been done via a prior AJAX call from the client.
            //    If `klarnaOrderId` is the `authorization_token`, then:
            //    order.AuthorizationTransactionId = klarnaOrderId;
            //    await _db.SaveChangesAsync();


            // The CaptureAsync method in KlarnaPaymentProvider is currently a placeholder.
            // It would need to be implemented to use the authorization_token to create an order with Klarna.
            var captureRequest = new CapturePaymentRequest { Order = order };
            var captureResult = await _paymentService.CaptureAsync(captureRequest);

            if (captureResult.Success && captureResult.NewPaymentStatus == PaymentStatus.Paid)
            {
                _logger.Information($"Klarna payment for order {order.Id} successfully captured. Klarna Order ID: {captureResult.CaptureTransactionId}");
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
            else
            {
                var errorStr = string.Join("; ", captureResult.Errors);
                _logger.Error($"Klarna Capture for order {order.Id} failed. Errors: {errorStr}");
                NotifyError($"Failed to finalize Klarna payment. {errorStr}");
                // Redirect to cart or payment failure page
                return RedirectToRoute("Cart");
            }
        }

        // Example: Klarna Notification URL endpoint (Webhook)
        // Klarna sends POST requests here for payment status updates.
        // The URL for this would be configured in KlarnaMerchantUrls.Notification
        [HttpPost]
        // [FromBodyJson] // If expecting JSON payload and have appropriate model. Requires custom model binder for x-www-form-urlencoded
        public async Task<IActionResult> Notification(/* KlarnaNotificationModel model */)
        {
            // TODO: Implement webhook handling
            // 1. Verify the notification (e.g., using a signature if Klarna provides one)
            // 2. Parse the notification payload
            // 3. Update order status based on the notification (e.g., if payment is captured, failed, etc.)
            //    - Find the order (e.g., by Klarna Order ID or your merchant_reference)
            //    - Potentially call _paymentService.ProcessPaymentAsync or adjust order status directly.
            //    - Be careful with order processing logic to avoid duplicate processing.

            string requestBody = string.Empty;
            using (var reader = new System.IO.StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            _logger.Information($"Klarna Notification received. Body: {requestBody}");

            // Klarna usually expects a 2xx response to acknowledge receipt.
            // E.g. HTTP 204 No Content for notifications.
            return NoContent();
        }

        // Add other actions as needed, e.g., for push notifications, authorization callbacks.
        // An endpoint might be needed for the Klarna JS SDK to send the authorization_token to.
        // For example:
        // [HttpPost]
        // public async Task<IActionResult> AuthorizePayment([FromBody] AuthorizePayload payload)
        // {
        //      var order = await _orderService.GetOrderByGuidAsync(payload.OrderGuid);
        //      if (order == null) return NotFound();
        //
        //      order.AuthorizationTransactionId = payload.AuthorizationToken;
        //      // Potentially store other info like Klarna Order ID if available at this stage.
        //      await _db.SaveChangesAsync();
        //
        //      // Optionally, you could trigger Capture here immediately if that's the desired flow.
        //      // Or, rely on the user being redirected to the Confirmation URL which then triggers Capture.
        //      return Json(new { success = true });
        // }
        // public class AuthorizePayload { public Guid OrderGuid { get; set; } public string AuthorizationToken { get; set; } }
    }
}
