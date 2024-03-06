using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingAddressRequirement : CheckoutRequirementBase
    {
        private static readonly FrozenSet<string> _actionNames = new[]
        {
            "ShippingAddress",
            "SelectShippingAddress"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShippingAddressRequirement(
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

        protected override string ActionName => "ShippingAddress";

        public override int Order => 20;

        public override bool IsRequirementFor(string action, string controller)
            => _actionNames.Contains(action) && controller.EqualsNoCase(ControllerName);

        public override async Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;
            var ga = customer.GenericAttributes;

            if (model != null 
                && model is int addressId 
                && IsSameRoute(HttpMethods.Post, "SelectShippingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address == null)
                {
                    return new(false);
                }

                customer.ShippingAddress = address;

                if (_shoppingCartSettings.QuickCheckoutEnabled)
                {
                    ga.DefaultShippingAddressId ??= customer.ShippingAddress.Id;
                }

                await _db.SaveChangesAsync();

                return new(true);
            }

            if (!cart.IsShippingRequired())
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
                var defaultAddress = customer.Addresses.FirstOrDefault(x => x.Id == customer.GenericAttributes.DefaultShippingAddressId);
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

            return new(await IsFulfilled(), null, skip);

            async Task<bool> IsFulfilled()
            {
                if (customer.ShippingAddressId == null)
                {
                    return false;
                }

                await _db.LoadReferenceAsync(customer, x => x.ShippingAddress);

                return customer.ShippingAddress != null;
            }
        }
    }
}
