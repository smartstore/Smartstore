using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class BillingAddressRequirement : CheckoutRequirementBase
    {
        private readonly SmartDbContext _db;

        public BillingAddressRequirement(SmartDbContext db, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _db = db;
        }

        protected override string ActionName => "BillingAddress";

        public override int Order => 10;

        public override async Task<(bool Fulfilled, CheckoutWorkflowError[] Errors)> IsFulfilledAsync(ShoppingCart cart, object model = null)
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

                    return (true, null);
                }

                return (false, null);
            }

            if (customer.BillingAddressId == null)
            {
                return (false, null);
            }

            await _db.LoadReferenceAsync(customer, x => x.BillingAddress);

            return (customer.BillingAddress != null, null);
        }
    }
}
