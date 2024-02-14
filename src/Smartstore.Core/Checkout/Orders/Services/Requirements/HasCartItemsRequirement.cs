using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class HasCartItemsRequirement : ICheckoutRequirement
    {
        public int Order => -1;

        public IActionResult Fulfill()
            => CheckoutWorkflow.RedirectToCart();

        public Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            return Task.FromResult(cart.HasItems);
        }
    }
}
