using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart.Events
{
    /// <summary>
    /// Represents a validating cart event
    /// </summary>
    public class ValidatingCartEvent
    {
        /// <summary>
        /// Creates a new <see cref="ValidatingCartEvent"/>.
        /// </summary>
        /// <param name="cart"><see cref="List{T}"/> of organized shopping cart items.</param>
        /// <param name="warnings"><see cref="List{T}"/> of warnings as strings.</param>
        /// <param name="customer">The current customer.</param>
        /// <remarks>
        /// Assign an <see cref="ActionResult"/> to <see cref="Result"/> to redirect the user, after the event has been completed.
        /// </remarks>
        public ValidatingCartEvent(
            IList<OrganizedShoppingCartItem> cart,
            IList<string> warnings,
            Customer customer)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(warnings, nameof(warnings));
            Guard.NotNull(customer, nameof(customer));

            Cart = cart;
            Warnings = warnings;
            Customer = customer;
        }

        /// <summary>
        /// Gets the customer
        /// </summary>
        public Customer Customer { get; init; }

        /// <summary>
        /// Gets organized shopping cart items
        /// </summary>
        public IList<OrganizedShoppingCartItem> Cart { get; init; }

        /// <summary>
        /// Gets warnings
        /// </summary>
        public IList<string> Warnings { get; init; }

        /// <summary>
        /// Gets or sets the result
        /// </summary>
        public ActionResult Result { get; set; }
    }
}