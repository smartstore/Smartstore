using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class BillingAddressRequirement : CheckoutRequirementBase
    {
        private readonly SmartDbContext _db;

        public BillingAddressRequirement(SmartDbContext db, IHttpContextAccessor httpContextAccessor)
            : base(CheckoutRequirement.BillingAddress, httpContextAccessor)
        {
            _db = db;
        }

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            if (cart.Customer.BillingAddressId == null)
            {
                return false;
            }

            await _db.LoadReferenceAsync(cart.Customer, x => x.BillingAddress);

            return cart.Customer.BillingAddress != null;
        }

        public override async Task<bool> AdvanceAsync(ShoppingCart cart, object model)
        {
            if (model is int addressId)
            {
                var address = cart.Customer.Addresses.FirstOrDefault(x => x.Id == addressId);
                if (address != null)
                {
                    cart.Customer.BillingAddress = address;
                    await _db.SaveChangesAsync();

                    return true;
                }
            }

            return false;
        }
    }
}
