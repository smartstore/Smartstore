using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Cart.Events
{
    /// <summary>
    /// Represents a rendering order totals event
    /// </summary>
    public class RenderingOrderTotalsEvent
    {
        /// <summary>
        /// Gets the customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Gets the store id
        /// </summary>
        public int? StoreId { get; set; }
    }
}