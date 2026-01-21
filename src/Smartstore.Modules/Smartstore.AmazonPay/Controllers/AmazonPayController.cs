using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Pay.API.WebStore.Buyer;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Http;
using Smartstore.Json;
using Smartstore.Web.Controllers;

namespace Smartstore.AmazonPay.Controllers;

public class AmazonPayController : PublicController
{
    private readonly SmartDbContext _db;
    private readonly IAmazonPayService _amazonPayService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ICheckoutStateAccessor _checkoutStateAccessor;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly ICheckoutWorkflow _checkoutWorkflow;
    private readonly IPaymentService _paymentService;
    private readonly AmazonPaySettings _settings;
    private readonly OrderSettings _orderSettings;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public AmazonPayController(
        SmartDbContext db,
        IAmazonPayService amazonPayService,
        IShoppingCartService shoppingCartService,
        ICheckoutStateAccessor checkoutStateAccessor,
        IOrderProcessingService orderProcessingService,
        ICheckoutWorkflow checkoutWorkflow,
        IPaymentService paymentService,
        AmazonPaySettings amazonPaySettings,
        OrderSettings orderSettings,
        ShoppingCartSettings shoppingCartSettings)
    {
        _db = db;
        _amazonPayService = amazonPayService;
        _shoppingCartService = shoppingCartService;
        _checkoutStateAccessor = checkoutStateAccessor;
        _orderProcessingService = orderProcessingService;
        _checkoutWorkflow = checkoutWorkflow;
        _paymentService = paymentService;
        _settings = amazonPaySettings;
        _orderSettings = orderSettings;
        _shoppingCartSettings = shoppingCartSettings;
    }

    /// <summary>
    /// AJAX. Creates the AmazonPay checkout session object after clicking the AmazonPay button.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCheckoutSession(ProductVariantQuery query, bool? useRewardPoints)
    {
        var signature = string.Empty;
        var payload = string.Empty;
        var message = string.Empty;
        var messageType = string.Empty;

        try
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            var warnings = new List<string>();

            // Save data entered on cart page.
            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints);
            if (isCartValid)
            {
                var client = HttpContext.GetAmazonPayApiClient(store.Id);
                var checkoutReviewUrl = Url.Action(nameof(CheckoutReview), "AmazonPay", null, currentScheme);
                var request = new CreateCheckoutSessionRequest(checkoutReviewUrl, _settings.ClientId)
                {
                    PlatformId = AmazonPayProvider.PlatformId
                };

                // TODO later: config for specialRestrictions 'RestrictPOBoxes', 'RestrictPackstations'.
                if (cart.HasItems && cart.IsShippingRequired)
                {
                    var allowedCountryCodes = await _db.Countries
                        .ApplyStandardFilter(false, store.Id)
                        .Where(x => x.AllowsBilling || x.AllowsShipping)
                        .Select(x => x.TwoLetterIsoCode)
                        .ToListAsync();

                    if (allowedCountryCodes.Any())
                    {
                        request.DeliverySpecifications.AddressRestrictions.Type = RestrictionType.Allowed;
                        allowedCountryCodes
                            .Distinct()
                            .Each(countryCode => request.DeliverySpecifications.AddressRestrictions.AddCountryRestriction(countryCode));
                    }
                }

                payload = request.ToJson();
                signature = client.GenerateButtonSignature(payload);
            }
            else
            {
                message = string.Join(Environment.NewLine, warnings);
                messageType = "warning";
            }
        }
        catch (Exception ex)
        {
            message = ex.Message;
            messageType = "error";
        }

        return Json(new
        {
            success = signature.HasValue(),
            signature,
            payload,
            message,
            messageType
        });
    }

    /// <summary>
    /// The buyer is redirected to this action method after they complete checkout on the AmazonPay hosted page.
    /// </summary>
    public async Task<IActionResult> CheckoutReview(string amazonCheckoutSessionId)
    {
        try
        {
            var cart = await _shoppingCartService.GetCartAsync(storeId: Services.StoreContext.CurrentStore.Id);
            var review = await ProcessCheckoutReview(cart, amazonCheckoutSessionId);
            if (review.Success)
            {
                var result = await _checkoutWorkflow.AdvanceAsync(new(cart, HttpContext, Url));
                if (result.ActionResult != null)
                {
                    return result.ActionResult;
                }

                var actionName = review.IsShippingMethodMissing
                    ? nameof(CheckoutController.ShippingMethod)
                    : nameof(CheckoutController.Confirm);

                return RedirectToAction(actionName, "Checkout");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            NotifyError(ex);
        }

        return RedirectToRoute("ShoppingCart");
    }

    private async Task<CheckoutReviewResult> ProcessCheckoutReview(ShoppingCart cart, string checkoutSessionId)
    {
        var result = new CheckoutReviewResult();
        var customer = cart.Customer;
        var ga = customer.GenericAttributes;

        if (checkoutSessionId.IsEmpty())
        {
            NotifyWarning(T("Payment.PaymentFailure"));
            return result;
        }

        result.IsShippingMethodMissing = cart.IsShippingRequired && ga.SelectedShippingOption == null;

        if (!cart.HasItems)
        {
            NotifyWarning(T("ShoppingCart.CartIsEmpty"));
            return result;
        }

        if (!_orderSettings.AnonymousCheckoutAllowed && !customer.IsRegistered())
        {
            NotifyWarning(T("Checkout.AnonymousNotAllowed"));
            return result;
        }

        await _db.LoadCollectionAsync(customer, x => x.Addresses);

        // Create addresses from AmazonPay checkout session.
        var client = HttpContext.GetAmazonPayApiClient(cart.StoreId);
        var session = client.GetCheckoutSession(checkoutSessionId);

        CheckoutAdressResult billTo = null;
        CheckoutAdressResult shipTo = null;

        if (cart.Requirements.HasFlag(CheckoutRequirements.BillingAddress))
        {
            if (session.BillingAddress == null)
            {
                // Orders without a billing address cannot be created.
                NotifyError(T("Plugins.Payments.AmazonPay.MissingBillingAddress"));
                return result;
            }

            billTo = await _amazonPayService.CreateAddressAsync(session, customer, true);
            if (!billTo.Success)
            {
                // We have to redirect the buyer back to the shopping cart because we cannot change the address at this stage.
                // We cannot store invalid addresses and assign them to a customer.
                NotifyWarning(T("Plugins.Payments.AmazonPay.BillingToCountryNotAllowed"));
                result.RequiresAddressUpdate = true;
                return result;
            }
        }

        if (cart.IsShippingRequired)
        {
            shipTo = await _amazonPayService.CreateAddressAsync(session, customer, false);
            if (!shipTo.Success)
            {
                NotifyWarning(T("Plugins.Payments.AmazonPay.ShippingToCountryNotAllowed"));
                result.RequiresAddressUpdate = true;
                return result;
            }
        }

        // Update customer.
        if (billTo != null)
        {
            var billingAddress = customer.FindAddress(billTo.Address);
            if (billingAddress != null)
            {
                customer.BillingAddress = billingAddress;
            }
            else
            {
                customer.Addresses.Add(billTo.Address);
                customer.BillingAddress = billTo.Address;
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                ga.DefaultBillingAddressId = customer.BillingAddress.Id;
            }
        }
        else
        {
            customer.BillingAddress = null;
        }

        if (shipTo != null)
        {
            var shippingAddress = customer.FindAddress(shipTo.Address);
            if (shippingAddress != null)
            {
                customer.ShippingAddress = shippingAddress;
            }
            else
            {
                customer.Addresses.Add(shipTo.Address);
                customer.ShippingAddress = shipTo.Address;
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                ga.DefaultShippingAddressId = customer.ShippingAddress.Id;
            }
        }
        else
        {
            customer.ShippingAddress = null;
        }

        ga.SelectedPaymentMethod = AmazonPayProvider.SystemName;

        if (_settings.CanSaveEmailAndPhone(customer.Email) && !customer.IsGuest())
        {
            customer.Email = session.Buyer.Email;
        }

        if (_settings.CanSaveEmailAndPhone(ga.Phone))
        {
            ga.Phone = billTo.Address.PhoneNumber.NullEmpty() ?? session.Buyer.PhoneNumber;
        }

        await _db.SaveChangesAsync();
        result.Success = true;

        var state = _checkoutStateAccessor.CheckoutState.GetCustomState<AmazonPayCheckoutState>();
        state.SessionId = checkoutSessionId;

        if (session.PaymentPreferences != null)
        {
            _checkoutStateAccessor.CheckoutState.PaymentSummary = string.Join(", ", session.PaymentPreferences.Select(x => x.PaymentDescriptor));
        }

        if (!HttpContext.Session.TryGetObject<ProcessPaymentRequest>(CheckoutState.OrderPaymentInfoName, out var paymentRequest)
            || paymentRequest == null
            || paymentRequest.OrderGuid == Guid.Empty)
        {
            HttpContext.Session.TrySetObject(CheckoutState.OrderPaymentInfoName, new ProcessPaymentRequest { OrderGuid = Guid.NewGuid() });
        }

        return result;
    }

    /// <summary>
    /// AmazonPay sends payment notifications (IPNs) to this action method.
    /// </summary>
    /// <remarks>
    /// INFO: multistore settings... because I keep stumbling over it.
    /// Is a mapping of a domain to the settings of the associated store: requested domain -> store entity -> multistore settings of the store.
    /// So LoadSettingsAsync(0) and LoadSettingsAsync(CurrentStore.Id) are the same thing.
    /// So for IPN-URLs it is not necessary to append the Store.Id (double information). The assignment to the store is always done via the domain.
    /// </remarks>
    [HttpPost, WebhookEndpoint]
    public async Task<IActionResult> IPNHandler()
    {
        try
        {
            Request.EnableBuffering();

            var node = await JsonNode.ParseAsync(Request.Body);
            Request.Body.Position = 0;

            if (node == null)
            {
                return Ok();
            }

            var ipnEnvelope = node.ToDynamic();

            var messageJson = (string)ipnEnvelope.Message;
            if (messageJson.IsEmpty())
            {
                return Ok();
            }

            var messageId = ipnEnvelope.MessageId.ToString();
            var message = JsonSerializer.Deserialize<IpnMessage>(messageJson, SmartJsonOptions.Default);

            if (message != null)
            {
                await ProcessIpn(message, messageId);
            }
        }
        catch (Exception ex)
        {
            if (Request.Body.CanSeek)
            {
                Request.Body.Position = 0;
            }

            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();

            Logger.Error(new($"{json}{Environment.NewLine}{Environment.NewLine}{ex}"), ex.Message);
        }

        return Ok();
    }

    private async Task ProcessIpn(IpnMessage message, string messageId)
    {
        string newState = null;
        var orderUpdated = false;
        var authorize = false;
        var paid = false;
        var voidOffline = false;
        var refund = false;
        var refundAmount = decimal.Zero;
        var chargeback = message.ObjectType.EqualsNoCase("CHARGEBACK");

        var chargePermissionId = message.ObjectType.EqualsNoCase("CHARGE_PERMISSION")
            ? message.ObjectId
            : message.ChargePermissionId;

        if (chargePermissionId.IsEmpty())
        {
            Logger.Warn(T("Plugins.Payments.AmazonPay.OrderNotFound", chargePermissionId.NaIfEmpty()));
            return;
        }

        var client = HttpContext.GetAmazonPayApiClient(Services.StoreContext.CurrentStore.Id);

        if (message.ObjectType.EqualsNoCase("CHARGE_PERMISSION"))
        {
            var response = client.GetChargePermission(message.ObjectId);
            if (response.Success)
            {
                var d = response.StatusDetails;
                newState = d.State.Grow(d.Reasons?.LastOrDefault()?.ReasonCode).Truncate(400);
                authorize = true;
                voidOffline = d.State.EqualsNoCase("Closed") || d.State.EqualsNoCase("NonChargeable");
            }
            else
            {
                Logger.Log(response);
            }
        }
        else if (message.ObjectType.EqualsNoCase("CHARGE"))
        {
            var response = client.GetCharge(message.ObjectId);
            if (response.Success)
            {
                var d = response.StatusDetails;
                var isDeclined = d.State.EqualsNoCase("Declined");
                newState = d.State.Grow(d.ReasonCode).Truncate(400);

                // Authorize if not still pending.
                authorize = !d.State.EqualsNoCase("AuthorizationInitiated");
                paid = d.State.EqualsNoCase("Captured");

                // "SoftDeclined... retry attempts may or may not be successful":
                // We can not distinguish in it in terms of further processing -> void payment.
                voidOffline = isDeclined || d.State.EqualsNoCase("Canceled");

                if (isDeclined && d.ReasonCode.EqualsNoCase("ProcessingFailure") && message.ChargePermissionId.HasValue())
                {
                    var response2 = client.GetChargePermission(message.ChargePermissionId);
                    if (response2.Success)
                    {
                        voidOffline = !response2.StatusDetails.State.EqualsNoCase("Chargeable");
                    }
                    else
                    {
                        Logger.Log(response2);
                    }
                }
            }
            else
            {
                Logger.Log(response);
            }
        }
        else if (message.ObjectType.EqualsNoCase("REFUND"))
        {
            var response = client.GetRefund(message.ObjectId);
            if (response.Success)
            {
                var d = response.StatusDetails;
                newState = d.State.Grow(d.ReasonCode).Truncate(400);
                refund = d.State.EqualsNoCase("Refunded");

                if (refund)
                {
                    refundAmount = response.RefundAmount.Amount;
                }
            }
            else
            {
                Logger.Log(response);
            }
        }

        // Perf. Jump out early from further processing.
        // Access the database only when necessary.
        if (!authorize && !paid && !voidOffline && !refund && !chargeback)
        {
            return;
        }

        // Get and process order.
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.PaymentMethodSystemName == AmazonPayProvider.SystemName && x.AuthorizationTransactionCode == chargePermissionId);
        if (order == null)
        {
            // In case of an authorization, the IPN may arrive shortly before order placement.
            if (!authorize)
            {
                Logger.Warn(T("Plugins.Payments.AmazonPay.OrderNotFound", chargePermissionId));
            }
            return;
        }

        if (!await _paymentService.IsPaymentProviderEnabledAsync(AmazonPayProvider.SystemName, order.StoreId))
        {
            return;
        }

        //Console.WriteLine($"AmazonPay {Request.Method} IPN. OrderId:{order.Id} {message.ObjectType} {newState} authorize:{authorize} paid:{paid} void:{voidOffline} refund:{refund} id:{message.ObjectId}");

        var oldState = order.CaptureTransactionResult.NullEmpty() ?? order.AuthorizationTransactionResult.NullEmpty() ?? "-";

        // INFO: order must be authorized for all other state changes.
        // That is why we call MarkAsAuthorizedAsync, even though the payment is not necessarily considered authorized at AmazonPay.
        if (authorize && order.CanMarkOrderAsAuthorized())
        {
            order.AuthorizationTransactionResult = newState;

            await _orderProcessingService.MarkAsAuthorizedAsync(order);
            orderUpdated = true;
        }

        if (paid && order.CanMarkOrderAsPaid())
        {
            order.CaptureTransactionResult = newState;

            await _orderProcessingService.MarkOrderAsPaidAsync(order);
            orderUpdated = true;
        }

        if (voidOffline && order.CanVoidOffline())
        {
            order.CaptureTransactionResult = newState;

            await _orderProcessingService.VoidOfflineAsync(order);
            orderUpdated = true;
        }

        if (refund && refundAmount > decimal.Zero)
        {
            var refundIds = order.GenericAttributes.Get<List<string>>(AmazonPayProvider.SystemName + ".RefundIds") ?? new List<string>();
            if (!refundIds.Contains(message.ObjectId, StringComparer.OrdinalIgnoreCase))
            {
                decimal receivable = order.OrderTotal - refundAmount;
                if (receivable <= decimal.Zero)
                {
                    if (order.CanRefundOffline())
                    {
                        order.CaptureTransactionResult = newState;

                        await _orderProcessingService.RefundOfflineAsync(order);
                        orderUpdated = true;
                    }
                }
                else
                {
                    if (order.CanPartiallyRefundOffline(refundAmount))
                    {
                        order.CaptureTransactionResult = newState;

                        await _orderProcessingService.PartiallyRefundOfflineAsync(order, refundAmount);
                        orderUpdated = true;
                    }
                }

                refundIds.Add(message.ObjectId);
                order.GenericAttributes.Set(AmazonPayProvider.SystemName + ".RefundIds", refundIds);
                await _db.SaveChangesAsync();
            }
        }

        // Add order note.
        if ((orderUpdated || chargeback) && _settings.AddOrderNotes)
        {
            var faviconUrl = WebHelper.GetAbsoluteUrl(
                Url.Content("~/Modules/Smartstore.AmazonPay/favicon.png"),
                HttpContext.Request,
                true);

            string note;

            if (chargeback)
            {
                note = T("Plugins.Payments.AmazonPay.IpnChargebackOrderNote",
                    messageId.NaIfEmpty(),
                    message.NotificationType, message.NotificationId,
                    message.ObjectType, message.ObjectId,
                    message.ChargePermissionId.NaIfEmpty());
            }
            else
            {
                note = T("Plugins.Payments.AmazonPay.IpnOrderNote",
                    messageId.NaIfEmpty(),
                    message.NotificationType, message.NotificationId,
                    message.ObjectType, message.ObjectId,
                    message.ChargePermissionId.NaIfEmpty(),
                    oldState, newState.NullEmpty() ?? "-");
            }

            order.OrderNotes.Add(new OrderNote
            {
                Note = $"<img src='{faviconUrl}' class='align-text-top mr-1' />" + note,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            order.HasNewPaymentNotification = true;

            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// The merchant is redirected to this action method after he clicked the "smart registration" button on AmazonPay configuration page.
    /// As a result of this registration, AmazonPay provides here JSON formatted API access keys.
    /// </summary>
    public async Task<IActionResult> ShareKey(string payload)
    {
        Response.Headers["Access-Control-Allow-Origin"] = "https://payments.amazon.com";
        Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
        Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

        try
        {
            await _amazonPayService.UpdateAccessKeysAsync(payload, 0);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { result = "error", message = ex.Message });
        }

        return Json(new { result = "success" });
    }

    #region Authentication\Sign-in

    /// <summary>
    /// AJAX. Creates the AmazonPay sign-in session object after clicking the AmazonPay button.
    /// </summary>
    [HttpPost]
    public IActionResult CreateSignInSession(string returnUrl)
    {
        var signature = string.Empty;
        var payload = string.Empty;
        var message = string.Empty;
        var messageType = string.Empty;

        try
        {
            var store = Services.StoreContext.CurrentStore;
            var currentScheme = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var client = HttpContext.GetAmazonPayApiClient(store.Id);
            var signInReturnUrl = Url.Action(nameof(SignIn), "AmazonPay", null, currentScheme);

            var request = new SignInRequest(signInReturnUrl, _settings.ClientId)
            {
                SignInScopes = new[]
                {
                    SignInScope.Name,
                    SignInScope.Email,
                    //SignInScope.PostalCode, 
                    //SignInScope.ShippingAddress,
                    //SignInScope.BillingAddress,
                    //SignInScope.PhoneNumber
                }
            };

            payload = request.ToJson();
            signature = client.GenerateButtonSignature(payload);

            HttpContext.Session.SetString("AmazonPayReturnUrl", returnUrl);
        }
        catch (Exception ex)
        {
            message = ex.Message;
            messageType = "error";
        }

        return Json(new
        {
            success = signature.HasValue(),
            signature,
            payload,
            message,
            messageType
        });
    }

    /// <summary>
    /// The buyer is redirected to this action method after they click the sign-in button.
    /// </summary>
    [Authorize(AuthenticationSchemes = "Smartstore.AmazonPay")]
    public IActionResult SignIn(/*string buyerToken*/)
    {
        var returnUrl = HttpContext.Session.GetString("AmazonPayReturnUrl");

        return RedirectToAction(nameof(IdentityController.ExternalLoginCallback), "Identity",
            new { provider = AmazonPaySignInProvider.SystemName, returnUrl });
    }

    #endregion
}
