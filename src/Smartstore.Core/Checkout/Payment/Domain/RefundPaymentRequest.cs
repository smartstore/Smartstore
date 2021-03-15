using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a request to refund a payment.
    /// </summary>
    public partial class RefundPaymentRequest : PaymentRequest
    {
        /// <summary>
        /// Gets or sets an amount.
        /// </summary>
        public Money AmountToRefund { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether it's a partial or a full refund.
        /// </summary>
        public bool IsPartialRefund { get; set; }
    }
}
