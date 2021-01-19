using System.Collections.Generic;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a default payment result
    /// </summary>
    public partial class PaymentResult
    {
        /// <summary>
        /// Gets or sets a payment status after processing.
        /// </summary>
        public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;

        /// <summary>
        /// Gets the list of errors
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets a value indicating whether errors list is empty.
        /// </summary>
        public bool Success 
            => Errors.Count == 0;
    }

    /// <summary>
    /// Represents a cancel recurring payment result.
    /// </summary>
    public partial class CancelRecurringPaymentResult : PaymentResult
    {
    }

    /// <summary>
    /// Represents a pre process payment result.
    /// </summary>
    public partial class PreProcessPaymentResult : PaymentResult
    {
    }

    /// <summary>
    /// Represents a void payment result.
    /// </summary>
    public partial class VoidPaymentResult : PaymentResult
    {
    }

    /// <summary>
    /// Represents a refund payment result.
    /// </summary>
    public partial class RefundPaymentResult : PaymentResult
    {
    }
}
