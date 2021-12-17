using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay payment button.
    /// </summary>
    public class AmazonPayButtonViewComponent : SmartViewComponent
    {
        private readonly IPaymentService _paymentService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly AmazonPaySettings _amazonPaySettings;

        public AmazonPayButtonViewComponent(
            IPaymentService paymentService,
            IShoppingCartService shoppingCartService,
            AmazonPaySettings amazonPaySettings)
        {
            _paymentService = paymentService;
            _shoppingCartService = shoppingCartService;
            _amazonPaySettings = amazonPaySettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;

            if (_amazonPaySettings.SellerId.IsEmpty() ||
                (_amazonPaySettings.ShowPayButtonForAdminOnly && !customer.IsAdmin()))
            {
                return Empty();
            }

            var cart = await _shoppingCartService.GetCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
            if (!cart.HasItems)
            {
                return Empty();
            }

            if (!await _paymentService.IsPaymentMethodActiveAsync(AmazonPayProvider.SystemName, cart, store.Id))
            {
                return Empty();
            }

            var model = new AmazonPayViewModel
            {
                SellerId = _amazonPaySettings.SellerId,
                ClientId = _amazonPaySettings.ClientId,
                // AmazonPay review: The setting for payment button type has been removed.
                ButtonType = "PwA",
                ButtonColor = _amazonPaySettings.PayButtonColor,
                ButtonSize = _amazonPaySettings.PayButtonSize,
                //...
            };

            return View(model);
        }
    }
}
