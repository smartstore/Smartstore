#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutContext
    {
        public CheckoutContext(HttpContext httpContext, ShoppingCart cart)
        {
            HttpContext = Guard.NotNull(httpContext);
            Cart = Guard.NotNull(cart);
        }

        public HttpContext HttpContext { get; }

        public RouteValueDictionary Route
            => HttpContext.Request.RouteValues;

        /// <summary>
        /// The shopping cart of the current customer.
        /// </summary>
        public ShoppingCart Cart { get; }

        /// <summary>
        /// An optional model (usually of a simple type) representing a user selection (e.g. address ID, shipping method ID or payment method system name).
        /// </summary>
        public object? Model { get; init; } = null;

        /// <summary>
        /// Gets a value indicating whether the current request corresponds to a specific route.
        /// </summary>
        public bool IsCurrentRoute(string? method, string action, string controller = "Checkout", string? area = null)
        {
            if (method.HasValue())
            {
                return HttpContext.Request.Method.EqualsNoCase(method) && Route.IsSameRoute(area, controller, action);
            }

            return Route.IsSameRoute(area, controller, action);
        }

        /// <summary>
        /// Gets a request form value.
        /// </summary>
        public string? GetFormValue(string key)
            => HttpContext.Request.Form.TryGetValue(key, out var val) ? val.ToString() : null;
    }
}
