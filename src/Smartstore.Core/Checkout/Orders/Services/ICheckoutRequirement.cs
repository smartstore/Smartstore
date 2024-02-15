#nullable enable

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Orders
{
    public enum CheckoutRequirement
    {
        BillingAddress = 10,
        ShippingAddress = 20,
        ShippingMethod = 30,
        PaymentMethod = 40
    }

    /// <summary>
    /// Represents a handler for a checkout requirement.
    /// The customer is redirected to the confirmation page if all requirements are fulfilled.
    /// </summary>
    /// <remarks>
    /// An <see cref="ICheckoutRequirement"/> attempts to fulfill the requirement automatically, if possible.
    /// </remarks>
    public interface ICheckoutRequirement
    {
        CheckoutRequirement Requirement { get; }

        /// <summary>
        /// Gets a value indicating whether the requirement is fulfilled.
        /// </summary>
        /// <param name="cart">The shopping cart of the current customer.</param>
        /// <returns><c>true</c> if the requirement is fulfilled, otherwise <c>false</c>.</returns>
        Task<bool> IsFulfilledAsync(ShoppingCart cart);

        IActionResult Fulfill();

        Task<bool> AdvanceAsync(ShoppingCart cart, object model);
    }
}
