#nullable enable

using Microsoft.AspNetCore.Mvc;

namespace Smartstore.Core.Checkout.Cart.Events
{
    /// <summary>
    /// Represents a validating cart event.
    /// </summary>
    public class ValidatingCartEvent
    {
        /// <summary>
        /// Creates a new <see cref="ValidatingCartEvent"/>.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="warnings">List of warnings.</param>
        /// <remarks>Assign an <see cref="ActionResult"/> to <see cref="Result"/> to redirect the user, after the event has been completed.</remarks>
        public ValidatingCartEvent(ShoppingCart cart, IList<string> warnings)
        {
            Guard.NotNull(cart.Customer);
            
            Cart = Guard.NotNull(cart);
            Warnings = Guard.NotNull(warnings);
        }

        /// <summary>
        /// Gets the shopping cart.
        /// </summary>
        public ShoppingCart Cart { get; init; }

        /// <summary>
        /// Gets warnings
        /// </summary>
        public IList<string> Warnings { get; init; }

        /// <summary>
        /// Gets or sets the result
        /// </summary>
        public IActionResult? Result { get; set; }
    }
}