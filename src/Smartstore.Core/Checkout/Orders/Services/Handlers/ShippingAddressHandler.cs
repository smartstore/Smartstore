using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(20, CheckoutActionNames.ShippingAddress, "SelectShippingAddress")]
    public class ShippingAddressHandler : ICheckoutHandler
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShippingAddressHandler(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        {
            var customer = context.Cart.Customer;
            var ga = customer.GenericAttributes;

            if (context.Model != null 
                && context.Model is int addressId 
                && context.IsCurrentRoute(HttpMethods.Post, "SelectShippingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address == null)
                {
                    return new(false);
                }

                customer.ShippingAddress = address;

                if (_shoppingCartSettings.QuickCheckoutEnabled)
                {
                    ga.DefaultShippingAddressId = customer.ShippingAddress.Id;
                }

                await _db.SaveChangesAsync();

                return new(true);
            }

            if (!context.Cart.IsShippingRequired)
            {
                if (customer.ShippingAddressId.GetValueOrDefault() != 0)
                {
                    customer.ShippingAddress = null;
                    await _db.SaveChangesAsync();
                }

                return new(true, null, true);
            }

            var state = _checkoutStateAccessor.CheckoutState;
            state.CustomProperties.TryGetValueAs("SkipShippingAddress", out bool skip);
            state.CustomProperties.Remove("SkipShippingAddress");

            if (!skip && _shoppingCartSettings.QuickCheckoutEnabled)
            {
                var defaultAddress = customer.Addresses.FirstOrDefault(x => x.Id == ga.DefaultShippingAddressId);
                if (defaultAddress != null)
                {
                    if (customer.ShippingAddressId != defaultAddress.Id)
                    {
                        customer.ShippingAddress = defaultAddress;
                        await _db.SaveChangesAsync();
                    }

                    return new(true);
                }
            }

            if (customer.ShippingAddressId == null)
            {
                return new(false, null, skip);
            }

            await _db.LoadReferenceAsync(customer, x => x.ShippingAddress);

            return new(customer.ShippingAddress != null, null, skip);
        }
    }
}
