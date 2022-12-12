using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Providers;
using Smartstore.StripeElements.Services;
using Smartstore.StripeElements.Settings;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;
using Stripe;

namespace Smartstore.StripeElements.Controllers
{
    public class StripeController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly StripeSettings _settings;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ITaxService _taxService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICurrencyService _currencyService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly StripeHelper _stripeHelper;
        
        public StripeController(
            SmartDbContext db, 
            StripeSettings settings, 
            ICheckoutStateAccessor checkoutStateAccessor,
            IShoppingCartService shoppingCartService,
            ITaxService taxService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IOrderCalculationService orderCalculationService,
            ICurrencyService currencyService,
            IOrderProcessingService orderProcessingService,
            StripeHelper stripeHelper)
        {
            _db = db;
            _settings = settings;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartService = shoppingCartService;
            _taxService = taxService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _orderCalculationService = orderCalculationService;
            _currencyService = currencyService;
            _orderProcessingService = orderProcessingService;
            _stripeHelper = stripeHelper;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent(string eventData, StripePaymentRequest paymentRequest)
        {
            var success = false;

            try
            {
                var returnedData = JsonConvert.DeserializeObject<PublicStripeEventModel>(eventData);

                // Create PaymentIntent.
                var options = new PaymentIntentCreateOptions
                {
                    Amount = paymentRequest.Total.Amount,
                    Currency = paymentRequest.Currency,
                    PaymentMethod = returnedData.PaymentMethod.Id,
                    CaptureMethod = _settings.CaptureMethod
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                // Save PaymentIntent in CheckoutState.
                var checkoutState = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                checkoutState.ButtonUsed = true;
                checkoutState.PaymentIntent = paymentIntent;

                // Create address if it doesn't exist.
                if (returnedData.PaymentMethod?.BillingDetails?.Address != null)
                {
                    var returnedAddress = returnedData.PaymentMethod?.BillingDetails?.Address;
                    var country = await _db.Countries
                        .AsNoTracking()
                        .Where(x => x.TwoLetterIsoCode.ToLower() == returnedAddress.Country.ToLower())
                        .FirstOrDefaultAsync();

                    var name = returnedData.PayerName.Split(" ");

                    var address = new Core.Common.Address
                    {
                        Email = returnedData.PayerEmail,
                        PhoneNumber = returnedData.PayerPhone,
                        FirstName = name[0],
                        LastName = name.Length > 1 ? name[1] : string.Empty,
                        City = returnedAddress.City,
                        CountryId = country.Id,
                        Address1 = returnedAddress.Line1,
                        Address2 = returnedAddress.Line2,
                        ZipPostalCode = returnedAddress.PostalCode
                    };

                    if (Services.WorkContext.CurrentCustomer.Addresses.FindAddress(address) == null)
                    {
                        Services.WorkContext.CurrentCustomer.Addresses.Add(address);
                        await _db.SaveChangesAsync();

                        Services.WorkContext.CurrentCustomer.BillingAddressId = address.Id;
                        await _db.SaveChangesAsync();
                    }
                }
                
                success = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
            }

            return Json(new { success });
        }

        [HttpPost]
        public async Task<IActionResult> GetUpdatePaymentRequest()
        {
            var success = false;

            var stripePaymentRequest = await _stripeHelper.GetStripePaymentRequestAsync();

            stripePaymentRequest.RequestPayerName = false;
            stripePaymentRequest.RequestPayerEmail = false;

            var paymentRequest = JsonConvert.SerializeObject(stripePaymentRequest);

            success = true;

            return Json(new { success, paymentRequest });
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

                paymentRequest.StoreId = store.Id;
                paymentRequest.CustomerId = customer.Id;
                paymentRequest.PaymentMethodSystemName = StripeElementsProvider.SystemName;

                // We must check here if an order can be placed to avoid creating unauthorized Sofort transactions.
                var (warnings, cart) = await _orderProcessingService.ValidateOrderPlacementAsync(paymentRequest);
                if (warnings.Count == 0)
                {
                    if (await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                    {
                        var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                        var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, true);

                        // Update Stripe Payment Intent.
                        var intentUpdateOptions = new PaymentIntentUpdateOptions
                        {
                            Amount = cartTotal.ConvertedAmount.Total.Value.RoundedAmount.ToSmallestCurrencyUnit(),
                            Currency = state.PaymentIntent.Currency,
                            PaymentMethod = state.PaymentMethod,
                        };

                        var service = new PaymentIntentService();
                        var paymentIntent = await service.UpdateAsync(state.PaymentIntent.Id, intentUpdateOptions);

                        var confirmOptions = new PaymentIntentConfirmOptions
                        {
                            ReturnUrl = store.GetHost(true) + Url.Action("RedirectionResult", "Stripe").TrimStart('/')
                        };

                        paymentIntent = await service.ConfirmAsync(paymentIntent.Id, confirmOptions);

                        if (paymentIntent.NextAction?.RedirectToUrl?.Url?.HasValue() == true)
                        {
                            redirectUrl = paymentIntent.NextAction.RedirectToUrl.Url;
                        }

                        success = true;
                        state.IsConfirmed = true;
                        state.FormData = formData.EmptyNull();
                    }
                    else
                    {
                        messages.Add(T("Checkout.MinOrderPlacementInterval"));
                    }
                }
                else
                {
                    messages.AddRange(warnings.Select(x => HtmlUtility.ConvertPlainTextToHtml(x)));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                messages.Add(ex.Message);
            }

            return Json(new { success, redirectUrl, messages });
        }

        public IActionResult RedirectionResult(string redirect_status)
        {
            var error = false;
            string message = null;
            var success = redirect_status == "succeeded" || redirect_status == "pending";

            if (success)
            {
                var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                if (state.PaymentIntent != null)
                {
                    state.SubmitForm = true;
                }
                else
                {
                    error = true;
                    message = T("Payment.MissingCheckoutState", "StripeCheckoutState." + nameof(state.PaymentIntent));
                }
            }
            else
            {
                error = true;
                message = T("Payment.PaymentFailure");
            }

            if (error)
            {
                _checkoutStateAccessor.CheckoutState.RemoveCustomState<StripeCheckoutState>();
                NotifyWarning(message);

                return RedirectToAction(nameof(CheckoutController.PaymentMethod), "Checkout");
            }

            return RedirectToAction(nameof(CheckoutController.Confirm), "Checkout");
        }

        [HttpPost]
        public IActionResult StorePaymentMethodId(string paymentMethodId)
        {
            var success = false;

            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
            state.PaymentMethod = paymentMethodId;

            success = true;

            return Json(new { success });
        }

        [HttpPost]
        [Route("stripe/webhookhandler")]
        public async Task<IActionResult> WebhookHandler()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var endpointSecret = _settings.WebhookSecret;

            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = Request.Headers["Stripe-Signature"];

                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, endpointSecret);

                if (stripeEvent.Type == Stripe.Events.PaymentIntentSucceeded)
                {
                    // Payment intent was captured in Stripe backend
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    // Get and process order.
                    var order = await _db.Orders.FirstOrDefaultAsync(x =>
                        x.PaymentMethodSystemName == StripeElementsProvider.SystemName
                        && x.AuthorizationTransactionId == paymentIntent.Id);

                    if (order != null)
                    {
                        // INFO: This can also be a partial capture.
                        var capturedAmount = paymentIntent.Amount;

                        // Convert ammount.
                        decimal convertedAmount = capturedAmount / 100M;

                        // Check if full order amount was captured.
                        if (order.OrderTotal == convertedAmount)
                        {
                            // Full capture.
                            order.PaymentStatus = PaymentStatus.Paid;
                        }
                        else
                        {
                            // Partial capture.
                            order.PaymentStatus = PaymentStatus.Pending;
                        }

                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        Logger.Warn(T("Plugins.Payments.Stripe.OrderNotFound", paymentIntent.Id));

                        return Ok();
                    }
                }
                else if (stripeEvent.Type == Stripe.Events.PaymentIntentCanceled)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    // Get and process order.
                    var order = await _db.Orders.FirstOrDefaultAsync(x =>
                        x.PaymentMethodSystemName == StripeElementsProvider.SystemName
                        && x.AuthorizationTransactionId == paymentIntent.Id);

                    if (order != null)
                    {
                        order.PaymentStatus = PaymentStatus.Voided;

                        // Write some infos into order notes.
                        WriteOrderNotes(order, paymentIntent.Charges.FirstOrDefault());

                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        Logger.Warn(T("Plugins.Payments.Stripe.OrderNotFound", paymentIntent.Id));

                        return Ok();
                    }
                }
                else if (stripeEvent.Type == Stripe.Events.ChargeRefunded)
                {
                    var charge = stripeEvent.Data.Object as Charge;

                    // Get and process order.
                    var order = await _db.Orders.FirstOrDefaultAsync(x =>
                        x.PaymentMethodSystemName == StripeElementsProvider.SystemName
                        && x.AuthorizationTransactionId == charge.PaymentIntentId);

                    if (order != null)
                    {
                        // INFO: This can also be a partial refund.
                        var capturedAmount = charge.Amount;

                        // Convert ammount.
                        decimal convertedAmount = capturedAmount / 100;

                        // Check if full order amount was refund.
                        if (order.OrderTotal == convertedAmount)
                        {
                            // Full refund.
                            order.PaymentStatus = PaymentStatus.Refunded;
                        }
                        else
                        {
                            // Partial refund.
                            order.PaymentStatus = PaymentStatus.PartiallyRefunded;
                        }

                        // Handle refunded amount.
                        order.RefundedAmount = convertedAmount;

                        // Write some infos into order notes.
                        WriteOrderNotes(order, charge);
                        
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        Logger.Warn(T("Plugins.Payments.Stripe.OrderNotFound", charge.PaymentIntentId));

                        return Ok();
                    }
                }
                else
                {
                    Logger.Warn(T("Unhandled event type: {0}", stripeEvent.Type));
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Logger.Error("Error: {0}", e.Message);
                return BadRequest();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        private static void WriteOrderNotes(Order order, Charge charge)
        {
            if (charge != null)
            {
                order.OrderNotes.Add(new OrderNote { DisplayToCustomer = true, Note = $"Reason: {charge.Refunds.FirstOrDefault().Reason}" });
                order.OrderNotes.Add(new OrderNote { DisplayToCustomer = true, Note = $"Reason: {charge.Description}" });
                order.OrderNotes.Add(new OrderNote { DisplayToCustomer = true, Note = $"Reason: {charge.Id}" });
            }
        }
    }
}