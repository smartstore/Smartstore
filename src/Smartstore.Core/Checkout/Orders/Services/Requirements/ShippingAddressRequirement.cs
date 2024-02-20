using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingAddressRequirement : CheckoutRequirementBase
    {
        private readonly SmartDbContext _db;

        public ShippingAddressRequirement(SmartDbContext db, IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
            _db = db;
        }

        protected override string ActionName => "ShippingAddress";

        public override int Order => 20;

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

                    return new(RequirementFulfilled.Yes);
                }

                return new(RequirementFulfilled.No);
            }

            if (cart.IsShippingRequired())
            {
                await _db.LoadReferenceAsync(customer, x => x.ShippingAddress);

                return new(customer.ShippingAddress != null);
            }
            else
            {
                if (customer.ShippingAddressId.GetValueOrDefault() != 0)
                {
                    customer.ShippingAddress = null;
                    await _db.SaveChangesAsync();
                }

                return new(RequirementFulfilled.Yes);
            }
        }
    }
}
