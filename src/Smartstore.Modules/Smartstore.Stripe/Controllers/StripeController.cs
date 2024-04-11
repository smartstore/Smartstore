using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Providers;
using Smartstore.StripeElements.Services;
using Smartstore.StripeElements.Settings;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;

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
        private readonly IRoundingHelper _roundingHelper;
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
            IRoundingHelper roundingHelper,
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
            _roundingHelper = roundingHelper;
            _orderProcessingService = orderProcessingService;
            _stripeHelper = stripeHelper;
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCart(ProductVariantQuery query, bool? useRewardPoints)
        {
            var success = false;
            var message = string.Empty;
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var warnings = new List<string>();
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);
            if (isCartValid)
            {
                success = true;
            }
            else
            {
                message = string.Join(Environment.NewLine, warnings);
            }

            return Json(new { success, message });
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

                    var name = returnedData.PayerName.Split(' ');

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

                    var customer = Services.WorkContext.CurrentCustomer;
                    if (customer.Addresses.FindAddress(address) == null)
                    {
                        customer.Addresses.Add(address);
                        await _db.SaveChangesAsync();

                        customer.BillingAddressId = address.Id;
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
        public async Task<IActionResult> GetUpdatePaymentRequest(ProductVariantQuery query, bool? useRewardPoints)
        {
            var success = false;
            var message = string.Empty;
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var warnings = new List<string>();
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            var isCartValid = await _shoppingCartService.SaveCartDataAsync(cart, warnings, query, useRewardPoints, false);
            if (isCartValid)
            {

                var stripePaymentRequest = await _stripeHelper.GetStripePaymentRequestAsync();

                stripePaymentRequest.RequestPayerName = false;
                stripePaymentRequest.RequestPayerEmail = false;

                var paymentRequest = JsonConvert.SerializeObject(stripePaymentRequest);

                return Json(new { success = true, paymentRequest });
            }
            else
            {
                message = string.Join(Environment.NewLine, warnings);
            }

            return Json(new { success, message });
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

                // We must check here if an order can be placed to avoid creating unauthorized transactions.
                var (warnings, cart) = await _orderProcessingService.ValidateOrderPlacementAsync(paymentRequest);
                if (warnings.Count == 0)
                {
                    if (await _orderProcessingService.IsMinimumOrderPlacementIntervalValidAsync(customer, store))
                    {
                        var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                        var cartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart, true);
                        var convertedTotal = cartTotal.ConvertedAmount.Total.Value;

                        var paymentIntentService = new PaymentIntentService();
                        PaymentIntent paymentIntent = null;

                        var shippingOption = customer.GenericAttributes.Get<ShippingOption>(SystemCustomerAttributeNames.SelectedShippingOption, store.Id);
                        var shipping = await GetShippingAddressAsync(customer, shippingOption.Name);

                        if (state.PaymentIntent == null)
                        {
                            paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                            {
                                Amount = _roundingHelper.ToSmallestCurrencyUnit(convertedTotal),
                                Currency = Services.WorkContext.WorkingCurrency.CurrencyCode.ToLower(),
                                CaptureMethod = _settings.CaptureMethod,
                                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                                {
                                    Enabled = true,
                                },
                                Metadata = new Dictionary<string, string>
                                {
                                    ["CustomerId"] = customer.Id.ToString()
                                },
                                PaymentMethod = state.PaymentMethod,
                                Shipping = shipping
                            });

                            state.PaymentIntent = paymentIntent;
                        }
                        else
                        {
                            // Update Stripe Payment Intent.
                            var intentUpdateOptions = new PaymentIntentUpdateOptions
                            {
                                Amount = _roundingHelper.ToSmallestCurrencyUnit(convertedTotal),
                                Currency = state.PaymentIntent.Currency,
                                PaymentMethod = state.PaymentMethod,
                                Shipping = shipping
                            };

                            paymentIntent = await paymentIntentService.UpdateAsync(state.PaymentIntent.Id, intentUpdateOptions);
                        }

                        var confirmOptions = new PaymentIntentConfirmOptions
                        {
                            ReturnUrl = store.GetAbsoluteUrl(Url.Action("RedirectionResult", "Stripe").TrimStart('/'))
                        };

                        paymentIntent = await paymentIntentService.ConfirmAsync(paymentIntent.Id, confirmOptions);

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

        private async Task<ChargeShippingOptions> GetShippingAddressAsync(Core.Identity.Customer customer, string carrier)
        {
            var address = customer.ShippingAddress ?? customer.BillingAddress;
            var country = await _db.Countries.FindAsync(address.CountryId);

            return new ChargeShippingOptions
            {
                Carrier = carrier,
                Name = $"{address.FirstName} {address.LastName}",
                Address = new AddressOptions
                {
                    City = address.City,
                    Country = country.TwoLetterIsoCode,
                    Line1 = address.Address1,
                    Line2 = address.Address2,
                    PostalCode = address.ZipPostalCode
                }
            };
        }

        public IActionResult RedirectionResult(string redirect_status)
        {
            var error = false;
            string message = null;
            var success = redirect_status == "succeeded" || redirect_status == "pending" || !redirect_status.HasValue();

            //Logger.LogInformation($"Stripe redirection result: '{redirect_status}'");

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
            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
            state.PaymentMethod = paymentMethodId;

            return Json(new { success = true });
        }

        [HttpPost]
        [Route("stripe/webhookhandler"), WebhookEndpoint]
        public async Task<IActionResult> WebhookHandler()
        {
            using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            var json = await reader.ReadToEndAsync();
            var endpointSecret = _settings.WebhookSecret;

            try
            {
                var signatureHeader = Request.Headers["Stripe-Signature"];

                // INFO: There should never be a version mismatch, as long as the hook was created in Smartstore backend.
                // But to keep even more stable we don't throw an exception on API version mismatch.
                var stripeEvent = EventUtility.ParseEvent(json, false);
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, endpointSecret, throwOnApiVersionMismatch: false);

                if (stripeEvent.Type == Stripe.Events.PaymentIntentSucceeded)
                {
                    // Payment intent was captured in Stripe backend
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var order = await GetStripeOrderAsync(paymentIntent.Id);

                    if (order != null)
                    {
                        // INFO: This can also be a partial capture.
                        decimal convertedAmount = paymentIntent.Amount / 100M;

                        // Check if full order amount was captured.
                        order.PaymentStatus = order.OrderTotal == convertedAmount ? PaymentStatus.Paid : PaymentStatus.Pending;

                        await _db.SaveChangesAsync();
                    }
                }
                else if (stripeEvent.Type == Stripe.Events.ChargeRefunded)
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    var order = await GetStripeOrderAsync(charge.PaymentIntentId);

                    if (order != null)
                    {
                        // INFO: This can also be a partial refund.
                        decimal convertedAmount = charge.Amount / 100M;

                        // Check if full order amount was refund.
                        order.PaymentStatus = order.OrderTotal == convertedAmount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

                        // Handle refunded amount.
                        order.RefundedAmount = convertedAmount;

                        // Write some infos into order notes.
                        WriteOrderNotes(order, charge);
                        
                        await _db.SaveChangesAsync();
                    }
                }
                else if (stripeEvent.Type == Stripe.Events.PaymentIntentCanceled)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var order = await GetStripeOrderAsync(paymentIntent.Id);

                    if (order != null)
                    {
                        order.PaymentStatus = PaymentStatus.Voided;

                        // Write some infos into order notes.
                        WriteOrderNotes(order, paymentIntent.LatestCharge);

                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    Logger.Warn("Unhandled Stripe event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                Logger.Error(ex);
                return BadRequest();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return StatusCode(500);
            }
        }

        private async Task<Order> GetStripeOrderAsync(string paymentIntentId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(x =>
                        x.PaymentMethodSystemName == StripeElementsProvider.SystemName &&
                        x.AuthorizationTransactionId == paymentIntentId);

            if (order == null)
            {
                Logger.Warn(T("Plugins.Smartstore.Stripe.OrderNotFound", paymentIntentId));
                return null;
            }

            return order;
        }

        // INFO: We leave this method in case we want to log further infos in future.
        private static void WriteOrderNotes(Order order, Charge charge)
        {
            if (charge != null)
            {
                order.AddOrderNote($"Reason for Charge-ID {charge.Id}: {charge.Refunds.FirstOrDefault().Reason} - {charge.Description}", true);
            }
        }
    }
}