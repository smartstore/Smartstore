using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Paystack.Configuration;
using Smartstore.Paystack.Providers;
using Smartstore.Web.Components;

namespace Smartstore.Paystack.Components
{
    public class PaystackViewComponent : SmartViewComponent
    {
        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly Lazy<IOrderCalculationService> _orderCalculationService;
        private readonly PaystackSettings _settings;
        private readonly OrderSettings _orderSettings;
        public PaystackViewComponent(
             IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            Lazy<IOrderCalculationService> orderCalculationService,
            PaystackSettings paystackSettings,
            OrderSettings orderSettings)
        {
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _orderCalculationService = orderCalculationService;
            _settings = paystackSettings;
            _orderSettings = orderSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            if (_settings.PublicKey.IsEmpty() ||
                _settings.PrivateKey.IsEmpty() ||
                _settings.BaseUrl.IsEmpty() ||
                (!_orderSettings.AnonymousCheckoutAllowed && customer.IsGuest()) || !customer.IsAdmin())
            {
                return Empty();
            }


            var store = Services.StoreContext.CurrentStore;
            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);

            if (!cart.HasItems || !await _paymentService.IsPaymentMethodActiveAsync(PaystackProvider.SystemName, cart, store.Id))
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

            return View();
        }
    }
}
