#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout requirement.
    /// The customer is redirected to the confirmation page if all requirements are fulfilled.
    /// </summary>
    /// <remarks>
    /// <see cref="ICheckoutRequirement"/> attempts to fulfill the requirement automatically, if possible.
    /// </remarks>
    public interface ICheckoutRequirement
    {
        /// <summary>
        /// Gets a value that corresponds to the order in which the requirements are checked,
        /// thus in which the associated checkout steps are completed.
        /// </summary>
        int Order { get; }

        bool IsRequirementFor(string action, string controller = "Checkout", string? area = null);

        /// <summary>
        /// Gets a value indicating whether the requirement is fulfilled.
        /// The user is redirected to <see cref="Fulfill"/> if the requirement is not fulfilled.
        /// </summary>
        /// <param name="cart">The shopping cart of the current customer.</param>
        /// <param name="model">
        /// An optional model (usually of a simple type) representing the data to fulfill the requirement.
        /// </param>
        /// <returns><c>true</c> if the requirement is fulfilled, otherwise <c>false</c>.</returns>
        Task<(bool Fulfilled, CheckoutWorkflowError[]? Errors)> IsFulfilledAsync(ShoppingCart cart, object? model = null);

        /// <summary>
        /// Gets the result to fulfill the requirement.
        /// </summary>
        IActionResult Fulfill();
    }
}
