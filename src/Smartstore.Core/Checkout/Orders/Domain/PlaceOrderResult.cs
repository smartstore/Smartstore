using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Orders
{
    /// <summary>
    /// Represents a PlaceOrderResult
    /// </summary>
    public partial class PlaceOrderResult
    {
        /// <summary>
        /// Gets or sets the placed order
        /// </summary>
        public Order PlacedOrder { get; set; }

        /// <summary>
        /// Gets or sets the list of errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Returns true if <see cref="Errors"/> does not contain any elements
        /// </summary>
        public bool Success => Errors.Count == 0;
    }
}