using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingAddressRequirement : CheckoutRequirementBase
    {
        private readonly SmartDbContext _db;

        public ShippingAddressRequirement(SmartDbContext db, IHttpContextAccessor httpContextAccessor)
            : base(CheckoutRequirement.ShippingAddress, httpContextAccessor)
        {
            _db = db;
        }

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            if (cart.IsShippingRequired())
            {
                await _db.LoadReferenceAsync(cart.Customer, x => x.ShippingAddress);

                return cart.Customer.ShippingAddress != null;
            }
            else
            {
                if (cart.Customer.ShippingAddressId.GetValueOrDefault() != 0)
                {
                    cart.Customer.ShippingAddress = null;
                    await _db.SaveChangesAsync();
                }

                return true;
            }
        }

        public override Task<IActionResult> AdvanceAsync(ShoppingCart cart, object model)
        {
            throw new NotImplementedException();
        }
    }
}
