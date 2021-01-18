using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a default payment request
    /// </summary>
    public partial class PaymentRequest
    {
        /// <summary>
        /// Gets or sets an order.
        /// </summary>
        public Order Order { get; set; }
    }
}
