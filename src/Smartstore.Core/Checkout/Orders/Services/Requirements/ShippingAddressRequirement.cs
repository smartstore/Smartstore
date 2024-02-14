using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class ShippingAddressRequirement : ICheckoutRequirement
    {
        private readonly SmartDbContext _db;

        public ShippingAddressRequirement(SmartDbContext db)
        {
            _db = db;
        }

        public static int CheckoutOrder => BillingAddressRequirement.CheckoutOrder + 10;
        public int Order => CheckoutOrder;

        public IActionResult Fulfill()
            => CheckoutWorkflow.RedirectToCheckout("ShippingAddress");

        public async Task<bool> IsFulfilledAsync(ShoppingCart cart)
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
    }
}
