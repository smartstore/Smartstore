using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders.Handlers
{
    public class ConfirmHandler : CheckoutHandlerBase
    {
        public ConfirmHandler(IHttpContextAccessor httpContextAccessor)
            : base(httpContextAccessor)
        {
        }

        protected override string ActionName => "Confirm";

        public override int Order => int.MaxValue;

        public override Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object model = null)
            => Task.FromResult(new CheckoutHandlerResult(true));
    }
}
