#nullable enable

using Microsoft.AspNetCore.Mvc;

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
        /// <returns><c>true</c> if the handler is associated with the action method, otherwise <c>false</c>.</returns>
        bool IsHandlerFor(CheckoutContext context);

        /// <summary>
        /// Processes a checkout step.
        /// </summary>
        Task<CheckoutHandlerResult> ProcessAsync(CheckoutContext context);

        /// <summary>
        /// Gets the <see cref="IActionResult"/> associated action method of the handler.
        /// </summary>
        IActionResult GetActionResult(CheckoutContext context);
    }
}
