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

    /// <summary>
    /// Represents a CancelRecurringPaymentResult.
    /// </summary>
    public partial class CancelRecurringPaymentRequest : PaymentRequest
    {
    }

    /// <summary>
    /// Represents a capture payment request.
    /// </summary>
    public partial class CapturePaymentRequest : PaymentRequest
    {
    }

    /// <summary>
    /// Represents a void payment request.
    /// </summary>
    public partial class VoidPaymentRequest : PaymentRequest
    {
    }
}
