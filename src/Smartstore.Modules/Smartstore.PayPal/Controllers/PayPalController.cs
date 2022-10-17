using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Controllers
{
    public class PayPalController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;

        public PayPalController(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            IShoppingCartService shoppingCartService,
            IOrderProcessingService orderProcessingService,
            PayPalHttpClient client,
            PayPalSettings settings)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartService = shoppingCartService;
            _orderProcessingService = orderProcessingService;
            _client = client;
            _settings = settings;
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

            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);
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

            return Json(new { success, message });
        }

        [HttpPost]
        [Route("paypal/webhookhandler")]
        public async Task<IActionResult> WebhookHandler()
        {
            string rawRequest = null;

            try
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    rawRequest = await reader.ReadToEndAsync();
                }

                if (rawRequest.HasValue())
                {
                    var webhookEvent = JsonConvert.DeserializeObject<WebhookEvent<WebhookResource>>(rawRequest);
                    var response = await VerifyWebhookRequest(Request, webhookEvent);
                    var resource = webhookEvent.Resource;

                    var customId = resource?.CustomId;
                    if (customId == null)
                    {
                        customId = resource?.PurchaseUnits[0]?.CustomId;
                    }
                    
                    var webhookResourceType = webhookEvent.ResourceType?.ToLowerInvariant();

                    if (!Guid.TryParse(customId, out var orderGuid))
                    {
                        return NotFound();
                    }

                    var order = await _db.Orders.FirstOrDefaultAsync(x => x.OrderGuid == orderGuid);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    // Add order note.
                    order.AddOrderNote($"Webhook: {Environment.NewLine}{rawRequest}", false);

                    // Handle transactions.
                    switch (webhookResourceType)
                    {
                        case "authorization":
                            await HandleAuthorizationAsync(order, resource);
                            break;
                        case "capture":
                            await HandleCaptureAsync(order, resource);
                            break;
                        case "refund":
                            await HandleRefundAsync(order, resource);
                            break;
                        default:
                            throw new IOException("Cannot proccess resource type.");
                    };

                    // Update order.
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, rawRequest);
            }

            return Ok();
        }

        private async Task HandleAuthorizationAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "created":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var authorizedAmount)
                        && authorizedAmount == Math.Round(order.OrderTotal, 2)
                        && order.CanMarkOrderAsAuthorized())
                    {
                        order.AuthorizationTransactionId = resource.Id;
                        order.AuthorizationTransactionResult = status;
                        await _orderProcessingService.MarkAsAuthorizedAsync(order);
                    }
                    break;

                case "denied":
                case "expired":
                case "pending":
                    order.CaptureTransactionResult = status;
                    order.OrderStatus = OrderStatus.Pending;
                    break;

                case "voided":
                    if (order.CanVoidOffline())
                    {
                        order.AuthorizationTransactionId = resource.Id;
                        order.AuthorizationTransactionResult = status;
                        await _orderProcessingService.VoidOfflineAsync(order);
                    }
                    break;
            }
        }

        private async Task HandleCaptureAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var capturedAmount))
                    {
                        if (order.CanMarkOrderAsPaid() && capturedAmount == Math.Round(order.OrderTotal, 2))
                        {
                            order.CaptureTransactionId = resource.Id;
                            order.CaptureTransactionResult = status;
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                    break;

                case "pending":
                    order.CaptureTransactionResult = status;
                    order.OrderStatus = OrderStatus.Pending;
                    break;

                case "declined":
                    order.CaptureTransactionResult = status;
                    order.OrderStatus = OrderStatus.Cancelled;
                    break;

                case "refunded":
                    if (order.CanRefundOffline())
                        await _orderProcessingService.RefundOfflineAsync(order);
                    break;
            }
        }

        private async Task HandleRefundAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    var refundIds = order.GenericAttributes.Get<List<string>>("Payments.PayPalStandard.RefundId") ?? new List<string>();
                    if (!refundIds.Contains(resource.Id)
                        && decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var refundedAmount)
                        && order.CanPartiallyRefundOffline(refundedAmount))
                    {
                        await _orderProcessingService.PartiallyRefundOfflineAsync(order, refundedAmount);
                        refundIds.Add(resource.Id);
                        order.GenericAttributes.Set("Payments.PayPalStandard.RefundId", refundIds);
                        await _db.SaveChangesAsync();
                    }
                    break;
            }
        }

        async Task<PayPalResponse> VerifyWebhookRequest(HttpRequest request, WebhookEvent<WebhookResource> webhookEvent)
        {
            var verifyRequest = new WebhookVerifySignatureRequest<WebhookResource>()
                .WithBody(new VerifyWebhookSignature<WebhookResource>
                {
                    AuthAlgo = request.Headers["PAYPAL-AUTH-ALGO"],
                    CertUrl = request.Headers["PAYPAL-CERT-URL"],
                    TransmissionId = request.Headers["PAYPAL-TRANSMISSION-ID"],
                    TransmissionSig = request.Headers["PAYPAL-TRANSMISSION-SIG"],
                    TransmissionTime = request.Headers["PAYPAL-TRANSMISSION-TIME"],
                    WebhookId = _settings.WebhookId,
                    WebhookEvent = webhookEvent
                });

            var response = await _client.ExecuteRequestAsync(verifyRequest);

            // TODO: (mh) (core) OK ain't enough. The response body must contain a "SUCCESS"
            // INFO: won't work for mockups that can be sent with PayPal Webhooks simulator as mockups can't be validated.
            if (response.Status == HttpStatusCode.OK)
            {
                return response;
            }
            else
            {
                throw new IOException("Could not verify request.");
            }
        }
    }
}