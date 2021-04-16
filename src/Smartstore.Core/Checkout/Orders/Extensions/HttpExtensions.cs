using Microsoft.AspNetCore.Http;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static class HttpExtensions
    {
        public static CheckoutState GetCheckoutState(this HttpContext httpContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));

            if (httpContext.Session.TryGetObject<CheckoutState>(CheckoutState.CheckoutStateSessionKey, out var state))
            {
                return state;
            }

            state = new CheckoutState();
            httpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, state);

            return state;
        }
    }
}
