using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common.Services;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    // TODO: (mg) (core) API design: don't mix up Pay and SignIn. Make two components & two views.
    
    /// <summary>
    /// Renders the AmazonPay checkout and sign-in button.
    /// </summary>
    public class AmazonPayButtonViewComponent : SmartViewComponent
    {
        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly AmazonPaySettings _settings;
        private readonly OrderSettings _orderSettings;

        public AmazonPayButtonViewComponent(
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            Lazy<ICurrencyService> currencyService,
            AmazonPaySettings amazonPaySettings,
            OrderSettings orderSettings)
        {
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _currencyService = currencyService;
            _settings = amazonPaySettings;
            _orderSettings = orderSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(string buttonType)
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var signIn = buttonType.EqualsNoCase("SignIn");

            if (_settings.PublicKeyId.IsEmpty() ||
                _settings.PrivateKey.IsEmpty() ||
                (!_orderSettings.AnonymousCheckoutAllowed && customer.IsGuest()) ||
                (_settings.ShowPayButtonForAdminOnly && !customer.IsAdmin() && !signIn))
            {
                return Empty();
            }

            ShoppingCart cart = null;

            if (!signIn)
            {
                cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                if (!cart.HasItems)
                {
                    return Empty();
                }

                buttonType ??= cart.IsShippingRequired() ? "PayAndShip" : "PayOnly";
            }

            if (!await _paymentService.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, cart, store.Id))
            {
                return Empty();
            }

            // INFO: we are "decoupling button render and checkout or sign-in initiation".
            // So we do not create and sign the payload here to reduce net traffic.

            var currencyCode = _currencyService.Value.PrimaryCurrency.CurrencyCode;
            var languageSeoCode = Services.WorkContext.WorkingLanguage.UniqueSeoCode;

            var model = new AmazonPayButtonModel(_settings, buttonType, currencyCode, languageSeoCode);

            return View(model);
        }
    }
}
