using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(10, "BillingAddress", "SelectBillingAddress")]
    public class BillingAddressHandler : ICheckoutHandler
    {
        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public BillingAddressHandler(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            ShoppingCartSettings shoppingCartSettings)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context)
        {
            var customer = context.Cart.Customer;
            var ga = customer.GenericAttributes;

            if (context.Model != null 
                && context.Model is int addressId 
                && context.IsCurrentRoute(HttpMethods.Post, "SelectBillingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address == null)
                {
                    return new(false);
                }

                var state = _checkoutStateAccessor.CheckoutState;
                var shippingAddressDiffers = context.GetFormValue("ShippingAddressDiffers")?.ToBool(true) ?? true;
                state.CustomProperties["SkipShippingAddress"] = !shippingAddressDiffers;

                customer.BillingAddress = address;
                customer.ShippingAddress = shippingAddressDiffers || !context.Cart.IsShippingRequired() ? null : address;

                if (_shoppingCartSettings.QuickCheckoutEnabled)
                {
                    ga.DefaultBillingAddressId = customer.BillingAddress.Id;
                    if (customer.ShippingAddress != null)
                    {
                        ga.DefaultShippingAddressId = customer.ShippingAddress.Id;
                    }
                }

                await _db.SaveChangesAsync();

                return new(true);
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                var defaultAddress = customer.Addresses.FirstOrDefault(x => x.Id == ga.DefaultBillingAddressId);
                if (defaultAddress != null)
                {
                    if (customer.BillingAddressId != defaultAddress.Id)
                    {
                        customer.BillingAddress = defaultAddress;
                        await _db.SaveChangesAsync();
                    }

                    return new(true);
                }
            }

            if (customer.BillingAddressId == null)
            {
                return new(false);
            }

            await _db.LoadReferenceAsync(customer, x => x.BillingAddress);

            return new(customer.BillingAddress != null);
        }
    }
}
