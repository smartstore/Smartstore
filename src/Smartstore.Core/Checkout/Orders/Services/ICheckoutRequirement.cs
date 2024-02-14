#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a checkout requirement. The customer is redirected to the confirmation page if all requirements are fulfilled.
    /// </summary>
    /// <remarks>
    /// An <see cref="ICheckoutRequirement"/> attempts to fulfill the requirement automatically, if possible.
    /// </remarks>
    public interface ICheckoutRequirement
    {
        /// <summary>
        /// Gets the ordinal number of the requiremment.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Gets a value indicating whether the requirement is fulfilled.
        /// </summary>
        /// <param name="cart">The shopping cart of the current customer.</param>
        /// <returns><c>true</c> if the requirement is fulfilled, otherwise <c>false</c>.</returns>
        Task<bool> IsFulfilledAsync(ShoppingCart cart);

        /// <summary>
        /// Gets the action result to fulfill the requirement.
        /// </summary>
        IActionResult Fulfill();
    }
}
