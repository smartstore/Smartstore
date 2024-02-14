using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class BillingAddressRequirement : ICheckoutRequirement
    {
        private readonly SmartDbContext _db;

        public BillingAddressRequirement(SmartDbContext db)
        {
            _db = db;
        }

        public static int CheckoutOrder => 10;
        public int Order => CheckoutOrder;

        public IActionResult Fulfill()
            => CheckoutWorkflow.RedirectToCheckout("BillingAddress");

        public async Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            if (cart.Customer.BillingAddressId == null)
            {
                return false;
            }

            await _db.LoadReferenceAsync(cart.Customer, x => x.BillingAddress);

            return cart.Customer.BillingAddress != null;
        }
    }
}
