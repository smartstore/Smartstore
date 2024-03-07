using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public class BillingAddressHandler : CheckoutHandlerBase
    {
        private static readonly FrozenSet<string> _actionNames = new[]
        {
            "BillingAddress",
            "SelectBillingAddress"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public BillingAddressHandler(
            SmartDbContext db,
            ICheckoutStateAccessor checkoutStateAccessor,
            IHttpContextAccessor httpContextAccessor,
            ShoppingCartSettings shoppingCartSettings)
            : base(httpContextAccessor)
        {
            _db = db;
            _checkoutStateAccessor = checkoutStateAccessor;
            _shoppingCartSettings = shoppingCartSettings;
        }

        protected override string ActionName => "BillingAddress";

        public override int Order => 10;

        public override bool IsHandlerFor(string action, string controller)
            => _actionNames.Contains(action) && controller.EqualsNoCase(ControllerName);

        public override async Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;
            var ga = customer.GenericAttributes;

            if (model != null 
                && model is int addressId 
                && IsSameRoute(HttpMethods.Post, "SelectBillingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address == null)
                {
                    return new(false);
                }

                var shippingAddressDiffers = GetFormValue("ShippingAddressDiffers").ToBool(true);
                _checkoutStateAccessor.CheckoutState.CustomProperties["SkipShippingAddress"] = !shippingAddressDiffers;

                customer.BillingAddress = address;
                customer.ShippingAddress = shippingAddressDiffers || !cart.IsShippingRequired() ? null : address;

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
