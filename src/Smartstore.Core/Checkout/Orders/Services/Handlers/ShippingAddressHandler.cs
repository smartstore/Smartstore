using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    [CheckoutStep(20, CheckoutActionNames.ShippingAddress, CheckoutActionNames.SelectShippingAddress)]
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
                && context.IsCurrentRoute(HttpMethods.Post, CheckoutActionNames.SelectShippingAddress))
            {
                if (!await SetShippingAddress(customer, addressId))
                {
                    return new(false);
                }

                await _db.LoadReferenceAsync(customer, x => x.ShippingAddress, false, q => q.Include(x => x.Country));
                var address = customer.ShippingAddress;

                if (_shoppingCartSettings.QuickCheckoutEnabled
                    && address.Country.AllowsShipping
                    && ga.DefaultShippingAddressId != address.Id)
                {
                    ga.DefaultShippingAddressId = address.Id;
                    await _db.SaveChangesAsync();
                }

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
            state.CustomProperties.TryGetValueAs("ShippingAddressDiffers", out bool addressDiffers);
            state.CustomProperties.Remove("SkipShippingAddress");
            state.CustomProperties.Remove("ShippingAddressDiffers");

            if (_shoppingCartSettings.QuickCheckoutEnabled 
                && !addressDiffers 
                && await SetShippingAddress(customer, ga.DefaultShippingAddressId ?? 0))
            {
                return new(true);
            }

            if (customer.ShippingAddressId == null)
            {
                return new(false, null, skip);
            }

            await _db.LoadReferenceAsync(customer, x => x.ShippingAddress, false, q => q.Include(x => x.Country));

            return new(customer.ShippingAddress != null, null, skip);
        }

        private async Task<bool> SetShippingAddress(Customer customer, int addressId)
        {
            var address = addressId != 0 ? customer.Addresses.FirstOrDefault(x => x.Id == addressId) : null;
            if (address == null)
            {
                return false;
            }

            await _db.LoadReferenceAsync(address, x => x.Country);

            if (address.Country.AllowsShipping)
            {
                if (customer.ShippingAddressId != address.Id)
                {
                    customer.ShippingAddress = address;
                    await _db.SaveChangesAsync();
                }

                return true;
            }
            else
            {
                if (customer.ShippingAddressId == address.Id)
                {
                    customer.ShippingAddress = null;
                    await _db.SaveChangesAsync();
                }

                return false;
            }
        }
    }
}
