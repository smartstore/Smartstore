using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents the result of an order placement.
    /// </summary>
    public partial class OrderPlacementResult
    {
        /// <summary>
        /// Gets or sets the placed order.
        /// </summary>
        public Order PlacedOrder { get; set; }

        /// <summary>
        /// Gets or sets a list of errors.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// <c>true</c> if <see cref="Errors"/> does not contain any elements.
        /// </summary>
        public bool Success
            => !Errors.Any();
    }
}