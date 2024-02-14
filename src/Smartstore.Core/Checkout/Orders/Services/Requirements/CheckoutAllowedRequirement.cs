using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Requirements
{
    public class CheckoutAllowedRequirement : ICheckoutRequirement
    {
        private readonly OrderSettings _orderSettings;

        public CheckoutAllowedRequirement(OrderSettings orderSettings)
        {
            _orderSettings = orderSettings;
        }

        public int Order => -1;

        public IActionResult Fulfill()
            => new ChallengeResult();

        public Task<bool> IsFulfilledAsync(ShoppingCart cart)
        {
            var allow = _orderSettings.AnonymousCheckoutAllowed || cart.Customer.IsRegistered();
            return Task.FromResult(allow);
        }
    }
}
