using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a default payment request.
    /// </summary>
    public partial class PaymentRequest
    {
        /// <summary>
        /// Gets or sets an order.
        /// </summary>
        public Order Order { get; init; }
    }

    /// <summary>
    /// Represents a recurring payment cancellation request.
    /// </summary>
    public partial class CancelRecurringPaymentRequest : PaymentRequest
    {
    }

    /// <summary>
    /// Represents a request to capture a payment.
    /// </summary>
    public partial class CapturePaymentRequest : PaymentRequest
    {
    }

    /// <summary>
    /// Represents a request to void a payment.
    /// </summary>
    public partial class VoidPaymentRequest : PaymentRequest
    {
    }
}
