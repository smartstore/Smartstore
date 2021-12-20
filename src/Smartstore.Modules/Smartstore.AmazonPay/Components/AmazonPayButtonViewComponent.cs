using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Web.Components;

namespace Smartstore.AmazonPay.Components
{
    /// <summary>
    /// Renders the AmazonPay payment ot login button.
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

            // TODO: (mg) (core) Every check which returns empty here can already be requested before registering the filter.
            // so theoretically there is no need to return Empty() from a payment button viewcomponent
            // RE: this is exactly what I don't want, to spread the logic for rendering over multiple callers. Not only the filter
            // invokes the component. The component alone should decide if and what to render.
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

            var model = new AmazonPayViewModel(_amazonPaySettings);

            return View(model);
        }
    }
}
