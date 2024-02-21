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

        public BillingAddressRequirement(SmartDbContext db, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _db = db;
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
                if (address != null)
                {
                    customer.BillingAddress = address;
                    await _db.SaveChangesAsync();

                    return new(RequirementFulfilled.Yes);
                }

                return new(RequirementFulfilled.No);
            }

            if (customer.BillingAddressId == null)
            {
                return new(RequirementFulfilled.No);
            }

            await _db.LoadReferenceAsync(customer, x => x.BillingAddress);

            return new(customer.BillingAddress != null);
        }
    }
}
