using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.PayPal.Client;
using Smartstore.PayPal.Client.Messages;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;

namespace Smartstore.PayPal.Controllers
{
    public class PayPalController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly PayPalHttpClient _client;
        private readonly PayPalSettings _settings;
        private readonly Currency _primaryCurrency;

        public PayPalController(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            IShoppingCartService shoppingCartService,
            IOrderProcessingService orderProcessingService,
            IRoundingHelper roundingHelper,
            PayPalHttpClient client,
            PayPalSettings settings,
            ICurrencyService currencyService)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartService = shoppingCartService;
            _orderProcessingService = orderProcessingService;
            _roundingHelper = roundingHelper;
            _client = client;
            _settings = settings;

            // INFO: Services wasn't resolved anymore in ctor.
            //_primaryCurrency = Services.CurrencyService.PrimaryCurrency;
            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        [HttpPost]
        public IActionResult InitTransaction(string orderId, string routeIdent)
        {
            var success = false;
            var message = string.Empty;

            if (!orderId.HasValue())
            {
                return Json(new { success, message = "No order id has been returned by PayPal." });
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var checkoutState = _checkoutStateAccessor.CheckoutState;

            // Only set this if we're not on payment page.
            if (routeIdent != "Checkout.PaymentMethod")
            {
                checkoutState.CustomProperties["PayPalButtonUsed"] = true;
            }

            // Store order id temporarily in checkout state.
            checkoutState.CustomProperties["PayPalOrderId"] = orderId;

            var paypalCheckoutState = checkoutState.GetCustomState<PayPalCheckoutState>();
            paypalCheckoutState.PayPalOrderId = orderId;

            var session = HttpContext.Session;

            if (!session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest) || processPaymentRequest == null)
            {
                processPaymentRequest = new ProcessPaymentRequest
                {
                    OrderGuid = Guid.NewGuid()
                };
            }

            processPaymentRequest.PayPalOrderId = orderId;
            processPaymentRequest.StoreId = Services.StoreContext.CurrentStore.Id;
            processPaymentRequest.CustomerId = customer.Id;
            processPaymentRequest.PaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;

            session.TrySetObject("OrderPaymentInfo", processPaymentRequest);

            success = true;

            return Json(new { success, message });
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(ProductVariantQuery query, bool? useRewardPoints, string paymentSource, string routeIdent = "")
        {
            var customer = Services.WorkContext.CurrentCustomer;

            // Only save cart data when we're on shopping cart page.
            if (routeIdent == "ShoppingCart.Cart")
            {
                var store = Services.StoreContext.CurrentStore;
                var warnings = new List<string>();
                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);

                if (!isCartValid)
                {
                    return Json(new { success = false, message = string.Join(Environment.NewLine, warnings) });
                }
            }

            var session = HttpContext.Session;

            if (!session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var processPaymentRequest) || processPaymentRequest == null)
            {
                processPaymentRequest = new ProcessPaymentRequest
                {
                    OrderGuid = Guid.NewGuid()
                };
            }

            session.TrySetObject("OrderPaymentInfo", processPaymentRequest);

            var selectedPaymentMethod = string.Empty;
            switch (paymentSource)
            {
                case "paypal-creditcard-hosted-fields-container":
                    selectedPaymentMethod = "Payments.PayPalCreditCard";
                    break;
                case "paypal-sepa-button-container":
                    selectedPaymentMethod = "Payments.PayPalSepa";
                    break;
                case "paypal-paylater-button-container":
                    selectedPaymentMethod = "Payments.PayPalPayLater";
                    break;
                case "paypal-button-container":
                default:
                    selectedPaymentMethod = "Payments.PayPalStandard";
                    break;
            }

            customer.GenericAttributes.SelectedPaymentMethod = selectedPaymentMethod;
            await customer.GenericAttributes.SaveChangesAsync();

            var orderMessage = await _client.GetOrderForStandardProviderAsync(processPaymentRequest.OrderGuid.ToString(), isExpressCheckout: true);
            var response = await _client.CreateOrderAsync(orderMessage);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            return Json(new { success = true, data = jResponse });
        }

        /// <summary>
        /// Called by ajax from credit card hosted fields to get two letter country code by id.
        /// </summary>
        /// <param name="id">The id of the selected country</param>
        /// <returns>ISO Code of the selected country</returns>
        [HttpPost]
        public async Task<IActionResult> GetCountryCodeById(int countryId)
        {
            var country = await _db.Countries.FindByIdAsync(countryId, false);
            var code = country?.TwoLetterIsoCode;

            return Json(code);
        }

        /// <summary>
        /// AJAX
        /// Called after buyer clicked buy-now-button but before the order was created.
        /// Processes payment and return redirect URL if there is any.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(string formData)
        {
            string redirectUrl = null;
            var messages = new List<string>();
            var success = false;

            try
            {
                var store = Services.StoreContext.CurrentStore;
                var customer = Services.WorkContext.CurrentCustomer;

                if (!HttpContext.Session.TryGetObject<ProcessPaymentRequest>("OrderPaymentInfo", out var paymentRequest) || paymentRequest == null)
                {
                    paymentRequest = new ProcessPaymentRequest();
                }

                await CreateOrderApmAsync(paymentRequest.OrderGuid.ToString());

                var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = customer.Id;
                paymentRequest.PaymentMethodSystemName = state.ApmProviderSystemName;

                // We must check here if an order can be placed to avoid creating unauthorized transactions.
                var (warnings, cart) = await _orderProcessingService.ValidateOrderPlacementAsync(paymentRequest);
                if (warnings.Count == 0)
                {
                    if (await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                    {
                        success = true;
                        state.IsConfirmed = true;
                        state.FormData = formData.EmptyNull();

                        paymentRequest.PayPalOrderId = state.PayPalOrderId;

                        HttpContext.Session.TrySetObject("OrderPaymentInfo", paymentRequest);

                        redirectUrl = state.ApmRedirectActionUrl;
                    }
                    else
                    {
                        messages.Add(T("Checkout.MinOrderPlacementInterval"));
                    }
                }
                else
                {
                    messages.AddRange(warnings.Select(HtmlUtility.ConvertPlainTextToHtml));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                messages.Add(ex.Message);
            }

            return Json(new { success, redirectUrl, messages });
        }

        private async Task CreateOrderApmAsync(string orderGuid)
        {
            var orderMessage = await _client.GetOrderForStandardProviderAsync(orderGuid, true, true);
            var checkoutState = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();

            // Get values from checkout input fields which were saved in CheckoutState.
            orderMessage.PaymentSource = GetPaymentSource(checkoutState);

            orderMessage.AppContext.Locale = Services.WorkContext.WorkingLanguage.LanguageCulture;

            // Get ReturnUrl & CancelUrl and add them to PayPalApplicationContext for proper redirection after payment was made or cancelled.
            var store = Services.StoreContext.CurrentStore;
            orderMessage.AppContext.ReturnUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionSuccess), "PayPal"));
            orderMessage.AppContext.CancelUrl = store.GetAbsoluteUrl(Url.Action(nameof(RedirectionCancel), "PayPal"));

            var response = await _client.CreateOrderAsync(orderMessage);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            // Save redirect url in CheckoutState.
            var status = (string)jResponse.status;
            checkoutState.PayPalOrderId = (string)jResponse.id;
            if (status == "PAYER_ACTION_REQUIRED")
            {
                var link = ((JObject)jResponse).SelectToken("links")
                            .Where(t => t["rel"].Value<string>() == "payer-action")
                            .First()["href"].Value<string>();

                checkoutState.ApmRedirectActionUrl = link;
            }
        }

        private PaymentSource GetPaymentSource(PayPalCheckoutState checkoutState)
        {
            var apmPaymentSource = new PaymentSourceApm
            {
                CountryCode = checkoutState.ApmCountryCode,
                Name = checkoutState.ApmFullname
            };

            var paymentSource = new PaymentSource();

            switch (checkoutState.ApmProviderSystemName)
            {
                case PayPalConstants.Giropay:
                    paymentSource.PaymentSourceGiroPay = apmPaymentSource;
                    break;
                case PayPalConstants.Bancontact:
                    paymentSource.PaymentSourceBancontact = apmPaymentSource;
                    break;
                case PayPalConstants.Blik:
                    paymentSource.PaymentSourceBlik = apmPaymentSource;
                    break;
                case PayPalConstants.Eps:
                    paymentSource.PaymentSourceEps = apmPaymentSource;
                    break;
                case PayPalConstants.Ideal:
                    paymentSource.PaymentSourceIdeal = apmPaymentSource;
                    break;
                case PayPalConstants.MyBank:
                    paymentSource.PaymentSourceMyBank = apmPaymentSource;
                    break;
                case PayPalConstants.Przelewy24:
                    paymentSource.PaymentSourceP24 = apmPaymentSource;
                    apmPaymentSource.Email = checkoutState.ApmEmail;
                    break;
                default:
                    break;
            }

            return paymentSource;
        }

        public IActionResult RedirectionSuccess()
        {
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<PayPalCheckoutState>();
            if (state.PayPalOrderId != null)
            {
                state.SubmitForm = true;
            }
            else
            {
                _checkoutStateAccessor.CheckoutState.RemoveCustomState<PayPalCheckoutState>();
                NotifyWarning(T("Payment.MissingCheckoutState", "PayPalCheckoutState." + nameof(state.PayPalOrderId)));

                return RedirectToAction(nameof(CheckoutController.PaymentMethod), "Checkout");
            }

            return RedirectToAction(nameof(CheckoutController.Confirm), "Checkout");
        }

        public IActionResult RedirectionCancel()
        {
            _checkoutStateAccessor.CheckoutState.RemoveCustomState<PayPalCheckoutState>();
            NotifyWarning(T("Payment.PaymentFailure"));

            return RedirectToAction(nameof(CheckoutController.PaymentMethod), "Checkout");
        }

        [HttpPost]
        [Route("paypal/webhookhandler"), WebhookEndpoint]
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

                    var webhookResourceType = webhookEvent.ResourceType?.ToLowerInvariant();

                    // We only handle authorization, capture, refund, checkout-order & order webhooks.
                    // checkout-order & order webhooks are used for APMs.
                    if (webhookResourceType != "authorization"
                        && webhookResourceType != "capture"
                        && webhookResourceType != "refund"
                        && webhookResourceType != "checkout-order"
                        && webhookResourceType != "order")
                    {
                        return Ok();
                    }

                    var customId = resource?.CustomId ?? resource?.PurchaseUnits?[0]?.CustomId;

                    if (!Guid.TryParse(customId, out var orderGuid))
                    {
                        return NotFound();
                    }

                    var order = await _db.Orders.FirstOrDefaultAsync(x => x.OrderGuid == orderGuid);

                    if (order == null)
                    {
                        return NotFound();
                    }

                    if (!order.PaymentMethodSystemName.StartsWith("Payments.PayPal"))
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
                        case "checkout-order":
                        case "order":
                            await HandleCheckoutOrderAsync(order, resource);
                            break;
                        default:
                            throw new PayPalException("Cannot proccess resource type.");
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
                        && authorizedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding)
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
                        if (order.CanMarkOrderAsPaid() && capturedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding))
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
                    order.PaymentStatus = PaymentStatus.Voided;
                    await _orderProcessingService.VoidOfflineAsync(order);
                    break;

                case "refunded":
                    if (order.CanRefundOffline())
                    {
                        await _orderProcessingService.RefundOfflineAsync(order);
                    }
                    break;
            }
        }

        private async Task HandleCheckoutOrderAsync(Order order, WebhookResource resource)
        {
            var status = resource?.Status.ToLowerInvariant();
            switch (status)
            {
                case "completed":
                    if (decimal.TryParse(resource.Amount?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var capturedAmount))
                    {
                        if (order.CanMarkOrderAsPaid() && capturedAmount == _roundingHelper.Round(order.OrderTotal, 2, _primaryCurrency.MidpointRounding))
                        {
                            order.CaptureTransactionId = resource.Id;
                            order.CaptureTransactionResult = status;
                            await _orderProcessingService.MarkOrderAsPaidAsync(order);
                        }
                    }
                    break;
                case "voided":
                    order.CaptureTransactionResult = status;
                    await _orderProcessingService.VoidAsync(order);
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
                throw new PayPalException("Could not verify request.");
            }
        }
    }
}