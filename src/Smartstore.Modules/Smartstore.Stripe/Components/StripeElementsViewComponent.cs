using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.StripeElements.Models;
using Smartstore.StripeElements.Services;
using Smartstore.StripeElements.Settings;
using Smartstore.Web.Components;

namespace Smartstore.StripeElements.Components
{
    public class StripeElementsViewComponent : SmartViewComponent
    {
        private readonly StripeSettings _settings;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ICurrencyService _currencyService;
        private readonly StripeHelper _stripeHelper;

        public StripeElementsViewComponent(
            StripeSettings settings,
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderCalculationService,
            ICheckoutStateAccessor checkoutStateAccessor,
            ICurrencyService currencyService,
            StripeHelper stripeHelper)
        {
            _settings = settings;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _checkoutStateAccessor = checkoutStateAccessor;
            _currencyService = currencyService;
            _stripeHelper = stripeHelper;
        }

        /// <summary>
        /// Renders Stripe Wallet Button Element in OffCanvasCart & cart page and Payment Element on payment selection page.
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // If public API key or secret API key haven't been configured yet, don't render anything.
            if (!_settings.PublicApiKey.HasValue() || !_settings.SecrectApiKey.HasValue())
            {
                return Empty();
            }

            var routeIdent = Request.RouteValues.GenerateRouteIdentifier();
            var isPaymentSelectionPage = routeIdent == "Checkout.PaymentMethod" || routeIdent == "Checkout.PaymentInfoAjax";

            var model = new PublicStripeElementsModel
            {
                PublicApiKey = _settings.PublicApiKey,
                IsPaymentSelectionPage = isPaymentSelectionPage
            };

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currency = Services.WorkContext.WorkingCurrency;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            // Get subtotal
            var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, true);
            var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithoutDiscount.Amount, currency);

            if (isPaymentSelectionPage)
            {
                var checkoutState = _checkoutStateAccessor.CheckoutState.GetCustomState<StripeCheckoutState>();
                PaymentIntent paymentIntent;

                if (checkoutState.PaymentIntent == null)
                {
                    var paymentIntentService = new PaymentIntentService();
                    paymentIntent = paymentIntentService.Create(new PaymentIntentCreateOptions
                    {
                        Amount = subTotalConverted.Amount.ToSmallestCurrencyUnit(),
                        Currency = Services.WorkContext.WorkingCurrency.CurrencyCode.ToLower(),
                        CaptureMethod = _settings.CaptureMethod,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                        {
                            Enabled = true,
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["CustomerId"] = customer.Id.ToString()
                        }
                    });

                    checkoutState.PaymentIntent = paymentIntent;
                }
                else
                {
                    paymentIntent = checkoutState.PaymentIntent;
                }
                
                model.ClientSecret = paymentIntent.ClientSecret;

                return View(model);
            }

            var stripePaymentRequest = await _stripeHelper.GetStripePaymentRequestAsync();

            model.PaymentRequest = JsonConvert.SerializeObject(stripePaymentRequest);

            return View(model);
        }
    }
}