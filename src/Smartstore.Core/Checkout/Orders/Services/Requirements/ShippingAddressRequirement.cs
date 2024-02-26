using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common;
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

            if (model != null 
                && model is int addressId 
                && IsSameRoute(HttpMethods.Post, "SelectShippingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address != null)
                {
                    customer.ShippingAddress = address;
                    await _db.SaveChangesAsync();

                    return new(true);
                }

                return new(false);
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

            if (_shoppingCartSettings.QuickCkeckoutEnabled)
            {
                var defaultAddressId = customer.GenericAttributes.DefaultShippingAddressId;
                var defaultAddress = customer.Addresses.FirstOrDefault(x => x.Id == defaultAddressId);
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
                return new(false);
            }

            await _db.LoadReferenceAsync(customer, x => x.ShippingAddress);

            var skip = false;
            var isFulfilled = customer.ShippingAddress != null;
            var state = _checkoutStateAccessor.CheckoutState;

            if (isFulfilled && state.CustomProperties.TryGetValueAs("SkipCheckoutShippingAddress", out skip))
            {
                state.CustomProperties.Remove("SkipCheckoutShippingAddress");
            }

            return new(isFulfilled, null, skip);
        }
    }
}
