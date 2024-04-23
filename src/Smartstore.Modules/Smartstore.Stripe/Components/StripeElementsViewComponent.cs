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
        private readonly ICurrencyService _currencyService;
        private readonly IRoundingHelper _roundingHelper;
        private readonly StripeHelper _stripeHelper;

        public StripeElementsViewComponent(
            StripeSettings settings,
            IShoppingCartService shoppingCartService,
            IOrderCalculationService orderCalculationService,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper,
            StripeHelper stripeHelper)
        {
            _settings = settings;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
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
                IsPaymentSelectionPage = isPaymentSelectionPage,
                IsCartPage = routeIdent == "ShoppingCart.Cart"
            };

            if (isPaymentSelectionPage)
            {
                var store = Services.StoreContext.CurrentStore;
                var customer = Services.WorkContext.CurrentCustomer;
                var currency = Services.WorkContext.WorkingCurrency;
                var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

                // Get subtotal
                var cartSubTotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, true);
                var subTotalConverted = _currencyService.ConvertFromPrimaryCurrency(cartSubTotal.SubtotalWithDiscount.Amount, currency);

                model.Amount = _roundingHelper.ToSmallestCurrencyUnit(subTotalConverted);
                model.Currency = currency.CurrencyCode.ToLower();
                model.CaptureMethod = _settings.CaptureMethod;

                return View(model);
            }

            var stripePaymentRequest = await _stripeHelper.GetStripePaymentRequestAsync();

            model.PaymentRequest = JsonConvert.SerializeObject(stripePaymentRequest);

            return View(model);
        }
    }
}