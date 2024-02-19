using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public override int Order => 10;

        protected override RedirectToActionResult FulfillResult
            => CheckoutWorkflow.RedirectToCheckout("BillingAddress");

        public override async Task<bool> IsFulfilledAsync(ShoppingCart cart, IList<CheckoutWorkflowError> errors, object model = null)
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

                    return true;
                }

                return false;
            }

            if (customer.BillingAddressId == null)
            {
                return false;
            }

            await _db.LoadReferenceAsync(customer, x => x.BillingAddress);

            return customer.BillingAddress != null;
        }
    }
}
