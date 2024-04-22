using Newtonsoft.Json.Linq;
using Smartstore.Core;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Cart.Events;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Shipping.Events;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Events;
using Smartstore.PayPal.Client;
using Smartstore.Utilities;

namespace Smartstore.PayPal
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly PayPalHttpClient _client;

        public Events(
            SmartDbContext db,
            ICommonServices services, 
            ICheckoutStateAccessor checkoutStateAccessor,
            PayPalHttpClient client)
        {
            _db = db;
            _services = services;
            _checkoutStateAccessor = checkoutStateAccessor;
            _client = client;
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
                .AddSequence(oldCart.Items.Select(x => x.Item.ProductId + x.Item.Quantity))
                .CombinedHash;

            var newHash = HashCodeCombiner
                .Start()
                .AddSequence(newCart.Items.Select(x => x.Item.ProductId + x.Item.Quantity))
                .CombinedHash;

            if (oldHash != newHash)
            {
                _checkoutStateAccessor.CheckoutState.CustomProperties["UserMustBeRedirectedToCart"] = true;
            }

            return;
        }

        public async Task HandleEvent(TrackingNumberAddedEvent eventMessage,
            PayPalSettings settings)
        {
            if (!settings.TransmitTrackingNumbers)
            {
                return;
            }

            var order = eventMessage.Shipment.Order;

            // Only do this if PayPal is the selected payment method of this order.
            if (order.PaymentMethodSystemName.HasValue() && order.PaymentMethodSystemName.StartsWith("Payments.PayPal"))
            {
                await AddTrackingNumberAsync(eventMessage.Shipment);
            }

            return;
        }

        public async Task HandleEvent(TrackingNumberChangedEvent eventMessage,
            PayPalHttpClient client,
            PayPalSettings settings)
        {
            if (!settings.TransmitTrackingNumbers)
            {
                return;
            }

            var order = eventMessage.Shipment.Order;

            // Only do this if PayPal is the selected payment method of this order.
            if (order.PaymentMethodSystemName.HasValue() && order.PaymentMethodSystemName.StartsWith("Payments.PayPal"))
            {
                // We can't change the tracking number in PayPal. We have to cancel the old tracking number and add a new one.
                await client.CancelTrackingNumberAsync(eventMessage.Shipment);

                await AddTrackingNumberAsync(eventMessage.Shipment);
            }

            return;
        }

        private async Task AddTrackingNumberAsync(Shipment shipment)
        {
            // Make API call to PayPal to add the tracking number.
            var response = await _client.AddTrackingNumberAsync(shipment);
            var rawResponse = response.Body<object>().ToString();
            dynamic jResponse = JObject.Parse(rawResponse);

            var trackingId = (string)jResponse.purchase_units[0].shipping.trackers[0].id;

            // Store tracking id as generic attribute in shipment.
            shipment.GenericAttributes.Set("PayPalTrackingId", trackingId);

            // Add order note.
            shipment.Order.AddOrderNote($"Tracking number {shipment.TrackingNumber} has been transmitted to PayPal. Tracking ID: {trackingId}");

            await _db.SaveChangesAsync();
        }
    }
}