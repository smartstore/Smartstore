using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(10, CheckoutActionNames.BillingAddress, CheckoutActionNames.SelectBillingAddress)]
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

        public async Task<CheckoutResult> ProcessAsync(CheckoutContext context)
        {
            var customer = context.Cart.Customer;
            var ga = customer.GenericAttributes;

            if (context.Model != null 
                && context.Model is int addressId 
                && context.IsCurrentRoute(HttpMethods.Post, CheckoutActionNames.SelectBillingAddress))
            {
                if (!await SetBillingAddress(customer, addressId))
                {
                    return new(false);
                }

                await _db.LoadReferenceAsync(customer, x => x.BillingAddress, false, q => q.Include(x => x.Country));
                var address = customer.BillingAddress;

                if (_shoppingCartSettings.QuickCheckoutEnabled)
                {
                    ga.DefaultBillingAddressId = address.Id;
                }

                // Shipping address.
                var shippingAddressDiffers = context.GetFormValue("ShippingAddressDiffers")?.ToBool(true) ?? true;
                var state = _checkoutStateAccessor.CheckoutState;
                state.CustomProperties["SkipShippingAddress"] = !shippingAddressDiffers && address.Country.AllowsShipping;
                state.CustomProperties["ShippingAddressDiffers"] = shippingAddressDiffers;

                if (shippingAddressDiffers || !context.Cart.IsShippingRequired)
                {
                    customer.ShippingAddress = null;
                }
                else if (address.Country.AllowsShipping)
                {
                    customer.ShippingAddress = address;

                    if (_shoppingCartSettings.QuickCheckoutEnabled)
                    {
                        ga.DefaultShippingAddressId = address.Id;
                    }
                }

                await _db.SaveChangesAsync();

                return new(true);
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled && await SetBillingAddress(customer, ga.DefaultBillingAddressId ?? 0))
            {
                return new(true);
            }

            if (customer.BillingAddressId == null)
            {
                return new(false);
            }

            await _db.LoadReferenceAsync(customer, x => x.BillingAddress, false, q => q.Include(x => x.Country));

            return new(customer.BillingAddress != null);
        }

        private async Task<bool> SetBillingAddress(Customer customer, int addressId)
        {
            var address = addressId != 0 ? customer.Addresses.FirstOrDefault(x => x.Id == addressId) : null;
            if (address == null)
            {
                return false;
            }

            await _db.LoadReferenceAsync(address, x => x.Country);

            if (address.Country.AllowsBilling)
            {
                if (customer.BillingAddressId != address.Id)
                {
                    customer.BillingAddress = address;
                    await _db.SaveChangesAsync();
                }

                return true;
            }
            else
            {
                if (customer.BillingAddressId == address.Id)
                {
                    customer.BillingAddress = null;
                    await _db.SaveChangesAsync();
                }

                return false;
            }
        }
    }
}
