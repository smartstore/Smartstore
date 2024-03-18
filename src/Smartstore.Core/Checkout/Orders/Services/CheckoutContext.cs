#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class CheckoutContext(ShoppingCart cart, HttpContext httpContext, IUrlHelper urlHelper)
    {
        public HttpContext HttpContext { get; } = Guard.NotNull(httpContext);
        public IUrlHelper UrlHelper { get; set; } = Guard.NotNull(urlHelper);

        /// <summary>
        /// The shopping cart of the current customer.
        /// </summary>
        public ShoppingCart Cart { get; } = Guard.NotNull(cart);

        public RouteValueDictionary RouteValues
            => HttpContext.Request.RouteValues;

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
                return HttpContext.Request.Method.EqualsNoCase(method) && RouteValues.IsSameRoute(area, controller, action);
            }

            return RouteValues.IsSameRoute(area, controller, action);
        }

        /// <summary>
        /// Gets a request form value.
        /// </summary>
        public string? GetFormValue(string key)
            => HttpContext.Request.Form.TryGetValue(key, out var val) ? val.ToString() : null;
    }
}
