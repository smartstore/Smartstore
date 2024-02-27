using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class BillingAddressRequirement : CheckoutRequirementBase
    {
        private static readonly FrozenSet<string> _actionNames = new[]
        {
            "BillingAddress",
            "SelectBillingAddress"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        private readonly SmartDbContext _db;
        private readonly ICheckoutStateAccessor _checkoutStateAccessor;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public BillingAddressRequirement(
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

        public override bool IsRequirementFor(string action, string controller)
            => _actionNames.Contains(action) && controller.EqualsNoCase(ControllerName);

        public override async Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object model = null)
        {
            var customer = cart.Customer;

            if (model != null 
                && model is int addressId 
                && IsSameRoute(HttpMethods.Post, "SelectBillingAddress"))
            {
                var address = customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address == null)
                {
                    return new(false);
                }

                var shippingAddressDiffers = GetFormValue("ShippingAddressDiffers").ToBool();

                customer.BillingAddress = address;
                customer.ShippingAddress = shippingAddressDiffers || !cart.IsShippingRequired() ? null : address;
                await _db.SaveChangesAsync();

                if (!shippingAddressDiffers)
                {
                    _checkoutStateAccessor.CheckoutState.CustomProperties["SkipCheckoutShippingAddress"] = true;
                }

                return new(true);
            }

            if (_shoppingCartSettings.QuickCheckoutEnabled)
            {
                var defaultAddressId = customer.GenericAttributes.DefaultBillingAddressId;
                var defaultAddress = customer.Addresses.FirstOrDefault(x => x.Id == defaultAddressId);
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
