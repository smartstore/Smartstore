#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders;

public partial class CheckoutContext(ShoppingCart cart, HttpContext httpContext, IUrlHelper urlHelper)
{
    private RouteValueDictionary? _routeValues;

    public HttpContext HttpContext { get; } = Guard.NotNull(httpContext);
    public IUrlHelper UrlHelper { get; set; } = Guard.NotNull(urlHelper);

    /// <summary>
    /// The shopping cart of the current customer.
    /// </summary>
    public ShoppingCart Cart { get; } = Guard.NotNull(cart);

    public RouteValueDictionary RouteValues
    {
        get => _routeValues ??= HttpContext.Request.RouteValues;
        set => _routeValues = Guard.NotNull(value);
    }

    /// <summary>
    /// An optional value that indicates which part of the checkout page needs to be refreshed.
    /// </summary>
    public CheckoutPartial? Part { get; init; } = null;

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
    public T? GetFormValue<T>(string key, T? defaultVal = default)
    {
        if (HttpContext.Request.Form.TryGetValue(key, out var val))
        {
            var valStr = val.ToString();
            if (valStr != null)
            {
                return valStr.Convert<T>() ?? defaultVal;
            }
        }

        return defaultVal;
    }

    /// <summary>
    /// Generates a URL with a path for an action method, which contains the specified
    /// <paramref name="action"/> name, <paramref name="controller"/> name, route <paramref name="values"/>, and
    /// <paramref name="protocol"/> to use.
    /// </summary>
    public string? Action(string? action, string? controller = "Checkout", object? values = null, string? protocol = null)
        => UrlHelper.Action(action, controller, values, protocol);
}

/// <summary>
/// Represents a part of a checkout page that needs to be refreshed.
/// </summary>
public enum CheckoutPartial
{
    OrderTotals,
    PaymentInfo
}