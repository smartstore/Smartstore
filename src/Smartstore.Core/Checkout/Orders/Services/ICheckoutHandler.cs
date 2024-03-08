#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Handles a checkout step.
    /// </summary>
    public interface ICheckoutHandler : IEquatable<ICheckoutHandler>
    {
        /// <summary>
        /// Gets a value that corresponds to the order in which the handlers are processed,
        /// thus in which the associated checkout steps are completed.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the handler is associated with an action method.
        /// </summary>
        /// <param name="action">Name of the action method.</param>
        /// <param name="controller">Name of the controller.</param>
        /// <returns><c>true</c> if the handler is associated with the action method, otherwise <c>false</c>.</returns>
        // TODO: (mg) Bad (inflexible) signature. Pass CheckoutContext instead. Members e.g. (all read-only): HttpContext, Action, Controller, Area, Module, ShoppingCart etc.
        bool IsHandlerFor(string action, string controller);

        /// <summary>
        /// Processes a checkout step.
        /// </summary>
        /// <param name="cart">The shopping cart (usually of the current customer).</param>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing a user selection (e.g. address ID, shipping method ID or payment method system name).
        /// </param>
        // TODO: (mg) Pass CheckoutContext also (as first param) and remove all IHttpContextAccessor ctor dependencies.
        Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object? model = null);

        /// <summary>
        /// Gets the <see cref="IActionResult"/> associated action method of the handler.
        /// </summary>
        // TODO: (mg) Pass CheckoutContext
        IActionResult GetActionResult();
    }
}
