namespace Smartstore.Core.Checkout.Payment
{
    public partial class PaymentConfirmationResult
    {
        /// <summary>
        /// Gets or sets the URL to which the user should be redirected for payment confirmation.
        /// </summary>
        public string RedirectUrl { get; init; }
    }

    /// <summary>
    /// Represents a default payment result.
    /// </summary>
    public partial class PaymentResult
    {
        [Obsolete("Throw a PaymentException if a payment error occurs.")]
        public List<string> Errors { get; set; }

        [Obsolete("Throw a PaymentException if a payment error occurs.")]
        public bool Success
            => Errors.Count == 0;
    }

    /// <summary>
    /// Represents a pre process payment result.
    /// </summary>
    public partial class PreProcessPaymentResult : PaymentResult
    {
    }

    /// <summary>
    /// Represents a recurring payment cancellation result.
    /// </summary>
    public partial class CancelRecurringPaymentResult : PaymentResult
    {
    }

    /// <summary>
    /// Represents a refund payment result.
    /// </summary>
    public partial class RefundPaymentResult : PaymentResult
    {
        /// <summary>
        /// Gets or sets a payment status after processing.
        /// </summary>
        public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;
    }

    /// <summary>
    /// Represents a void payment result.
    /// </summary>
    public partial class VoidPaymentResult : PaymentResult
    {
        /// <summary>
        /// Gets or sets a payment status after processing.
        /// </summary>
        public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;
    }
}
