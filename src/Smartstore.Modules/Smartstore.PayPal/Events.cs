using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Events;
using Smartstore.Utilities;

namespace Smartstore.PayPal
{
    public class Events : IConsumer
    {
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;

        public Events(ICommonServices services, ICheckoutStateAccessor checkoutStateAccessor)
        {
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;
        }

        public void HandleEvent(CustomerSignedInEvent eventMessage)
        {
            var customerBeforeLogin = _services.WorkContext.CurrentCustomer;
            
            if (customerBeforeLogin.GenericAttributes.SelectedPaymentMethod.HasValue() 
                && customerBeforeLogin.GenericAttributes.SelectedPaymentMethod.StartsWith("Payments.PayPal"))
            {
                // Save selected payment method in session                
                _checkoutStateAccessor.CheckoutState.CustomProperties["SelectedPaymentMethod"] = customerBeforeLogin.GenericAttributes.SelectedPaymentMethod;
            }

            return;
        }

        public async Task HandleEvent(MigrateShoppingCartEvent eventMessage,
            IShoppingCartService shoppingCartService)
        {
            // Only do this if PayPal is selected as payment method.
            if (!_checkoutStateAccessor.CheckoutState.CustomProperties.ContainsKey("SelectedPaymentMethod"))
            {
                return;
            }    

            // If migrated shopping cart differs from current shopping cart, store a value in session and redirect user to basket.
            var storeId = _services.StoreContext.CurrentStore.Id;
            var oldCart = await shoppingCartService.GetCartAsync(eventMessage.FromCustomer, storeId: storeId);
            var newCart = await shoppingCartService.GetCartAsync(eventMessage.ToCustomer, storeId: storeId);

            var oldHash = HashCodeCombiner
                .Start()
                .Add(oldCart.Items.Select(x => (x.Item.ProductId + x.Item.Quantity).GetHashCode()))
                .CombinedHash;

            var newHash = HashCodeCombiner
                .Start()
                .Add(newCart.Items.Select(x => (x.Item.ProductId + x.Item.Quantity).GetHashCode()))
                .CombinedHash;

            if (oldHash != newHash)
            {
                _checkoutStateAccessor.CheckoutState.CustomProperties["UserMustBeRedirectedToCart"] = true;
            }

            return;
        }
    }
}