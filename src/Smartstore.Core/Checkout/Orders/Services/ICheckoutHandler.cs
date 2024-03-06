#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Handles a checkout step.
    /// The customer is redirected to the confirmation page if all handlers are processed and quick checkout is enabled.
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
        bool IsHandlerFor(string action, string controller);

        /// <summary>
        /// Processes a checkout step.
        /// </summary>
        /// <param name="cart">The shopping cart (usually of the current customer).</param>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing a user selection (e.g. address ID, shipping method ID or payment method system name).
        /// </param>
        Task<CheckoutHandlerResult> ProcessAsync(ShoppingCart cart, object? model = null);

        /// <summary>
        /// Gets the <see cref="IActionResult"/> associated action method of the handler.
        /// </summary>
        IActionResult GetActionResult();
    }

    public partial class CheckoutHandlerResult(bool success, CheckoutWorkflowError[]? errors = null, bool skipPage = false)
    {
        /// <summary>
        /// Gets a value indicating whether the processing of the handler was successful.
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        /// Gets a value indicating whether the associated checkout page should be skipped.
        /// </summary>
        public bool SkipPage { get; set; } = skipPage;

        public CheckoutWorkflowError[] Errors { get; } = errors ?? [];
    }
}
