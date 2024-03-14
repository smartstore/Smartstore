using Microsoft.AspNetCore.Mvc;
using Smartstore.AmazonPay.Services;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay payment button.
    /// </summary>
    public class PayButtonViewComponent : SmartViewComponent
    {
        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly Lazy<IOrderCalculationService> _orderCalculationService;
        private readonly AmazonPaySettings _settings;
        private readonly OrderSettings _orderSettings;

        public PayButtonViewComponent(
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            Lazy<IOrderCalculationService> orderCalculationService,
            AmazonPaySettings amazonPaySettings,
            OrderSettings orderSettings)
        {
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = amazonPaySettings;
            _orderSettings = orderSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (_settings.PublicKeyId.IsEmpty() ||
                _settings.PrivateKey.IsEmpty() ||
                (!_orderSettings.AnonymousCheckoutAllowed && !customer.IsRegistered()) ||
                (_settings.ShowPayButtonForAdminOnly && !customer.IsAdmin()))
            {
                return Empty();
            }

            var currencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            if (!AmazonPayService.IsLedgerCurrencySupported(currencyCode))
            {
                return Empty();
            }

            var store = Services.StoreContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.HasItems || !await _paymentService.IsPaymentProviderActiveAsync(AmazonPayProvider.SystemName, cart, store.Id))
            {
                return Empty();
            }

            // Do not render AmazonPay button if there's nothing to pay.
            // Avoids InvalidParameterValue: The value '0' provided for 'chargeAmount.Amount' is invalid.
            var cartTotal = (Money?)await _orderCalculationService.Value.GetShoppingCartTotalAsync(cart);
            if (cartTotal.HasValue && cartTotal.Value == decimal.Zero)
            {
                return Empty();
            }

            var model = new AmazonPayButtonModel(
                _settings,
                cart.IsShippingRequired ? "PayAndShip" : "PayOnly",
                currencyCode,
                Services.WorkContext.WorkingLanguage.UniqueSeoCode);

            return View(model);
        }
    }
}
