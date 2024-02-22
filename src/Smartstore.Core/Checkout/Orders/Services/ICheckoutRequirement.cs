#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout requirement.
    /// The customer is redirected to the confirmation page if all requirements are fulfilled and quick checkout is enabled.
    /// </summary>
    /// <remarks>
    /// An <see cref="ICheckoutRequirement"/> implementation attempts to fulfill the requirement automatically, if possible.
    /// </remarks>
    public interface ICheckoutRequirement : IEquatable<ICheckoutRequirement>
    {
        /// <summary>
        /// Gets a value that corresponds to the order in which the requirements are checked,
        /// thus in which the associated checkout steps are completed.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the requirement is associated with an action method.
        /// </summary>
        /// <param name="action">Name of the action method.</param>
        /// <param name="controller">Name of the controller.</param>
        /// <returns><c>true</c> if the requirement is associated with the action method, otherwise <c>false</c>.</returns>
        bool IsRequirementFor(string action, string controller);

        /// <summary>
        /// Checks whether a requirement is fulfilled.
        /// </summary>
        /// <param name="cart">The shopping cart (usually of the current customer).</param>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing the data to fulfill the requirement (typically in POST requests).
        /// </param>
        Task<CheckoutRequirementResult> CheckAsync(ShoppingCart cart, object? model = null);

        /// <summary>
        /// Gets an <see cref="IActionResult"/> to fulfill the requirement.
        /// </summary>
        IActionResult Fulfill();
    }

    public partial class CheckoutRequirementResult(bool isFulfilled, CheckoutWorkflowError[]? errors = null, bool skipPage = false)
    {
        /// <summary>
        /// /// Gets a value indicating whether the requirement is fulfilled.
        /// </summary>
        public bool IsFulfilled { get; } = isFulfilled;

        /// <summary>
        /// Gets a value indicating whether the associated checkout page should be skipped.
        /// </summary>
        public bool SkipPage { get; set; } = skipPage;

        public CheckoutWorkflowError[] Errors { get; } = errors ?? [];
    }
}
