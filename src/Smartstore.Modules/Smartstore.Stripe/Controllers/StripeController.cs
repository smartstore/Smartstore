using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
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
using Smartstore.Core.Localization;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Providers;
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
            IOrderProcessingService orderProcessingService)
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

            // TODO: (mh) (core) Create helper class for this as its also used in Viewcomponent.

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currency = Services.WorkContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            // Get subtotal
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, true);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            var cartProducts = cart.Items.Select(x => x.Item.Product).ToArray();
            var batchContext = _productService.CreateProductBatchContext(cartProducts, null, customer, false);
            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, batchContext);

            var displayItems = new List<StripePaymentItem>();

            foreach (var item in cart.Items)
            {
                var taxRate = await _taxService.GetTaxRateAsync(item.Item.Product);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(item, calculationOptions);
                var (unitPrice, subtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                displayItems.Add(new StripePaymentItem
                {
                    Label = item.Item.Product.GetLocalized(x => x.Name),
                    Amount = subtotal.FinalPrice.Amount.ToSmallestCurrencyUnit(),
                    Pending = false
                });
            }

            // Prepare Stripe payment request object.
            var stripePaymentRequest = new StripePaymentRequest
            {
                Country = Services.WorkContext.WorkingLanguage.UniqueSeoCode.ToUpper(),
                Currency = Services.WorkContext.WorkingCurrency.CurrencyCode.ToLower(),
                Total = new StripePaymentItem
                {
                    Amount = subTotalConverted.Amount.ToSmallestCurrencyUnit(),
                    Label = T("Order.SubTotal").Value,
                    Pending = false
                },
                DisplayItems = displayItems,
                // INFO: this differs from initial request
                RequestPayerName = false,
                RequestPayerEmail = false
            };

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
                            Amount = cartTotal.Total.Value.Amount.ToSmallestCurrencyUnit(),
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
            var success = redirect_status == "succeeded";

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
        public async Task<IActionResult> StorePaymentMethodId(string paymentMethodId)
        {
            var success = false;

            //var client = new StripeClient(_settings.SecrectApiKey);
            //var service = new PaymentMethodService(client);

            //var paymentMethod = await service.CreateAsync(
            //    new PaymentMethodCreateOptions
            //    {
            //        PaymentMethod = selectedPayment,
            //    }
            //);

            var state = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
            state.PaymentMethod = paymentMethodId;

            success = true;

            return Json(new { success });
        }

        [HttpPost]
        [Route("stripe/webhookhandler")]
        public async Task<IActionResult> WebhookHandler()
        {
            // TODO: (mh) (core) Implement

            return Ok();
        }
    }
}