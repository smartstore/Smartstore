namespace Smartstore.Core.Checkout.Payment;

/// <summary>
/// Represents a process payment result.
/// </summary>
public partial class ProcessPaymentResult : PaymentResult
{
    /// <summary>
    /// Gets or sets an AVS result.
    /// </summary>
    public string AvsResult { get; set; }

    /// <summary>
    /// Gets or sets the ID of a payment authorization.
    /// Usually this comes from a payment gateway.
    /// </summary>
    public string AuthorizationTransactionId { get; set; }

    /// <summary>
    /// Gets or sets a payment transaction code.
    /// Not used by Smartstore. Can be any data that the payment provider needs for later processing.
    /// </summary>
    public string AuthorizationTransactionCode { get; set; }

    /// <summary>
    /// Gets or sets a short result info about the payment authorization.
    /// </summary>
    public string AuthorizationTransactionResult { get; set; }

    /// <summary>
    /// Gets or sets the ID of a payment capture.
    /// Usually this comes from a payment gateway. Can be equal to <see cref="AuthorizationTransactionId"/>.
    /// </summary>
    public string CaptureTransactionId { get; set; }

    /// <summary>
    /// Gets or sets a short result info about the payment capture.
    /// </summary>
    public string CaptureTransactionResult { get; set; }

    /// <summary>
    /// Gets or sets the ID for payment subscription. Usually used for recurring payment.
    /// </summary>
    public string SubscriptionTransactionId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether storing of credit card number, CVV2 is allowed.
    /// </summary>
    public bool AllowStoringCreditCardNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether storing of credit card number, CVV2 is allowed.
    /// </summary>
    public bool AllowStoringDirectDebit { get; set; }

    /// <summary>
    /// Gets or sets a payment status after processing.
    /// </summary>
    public PaymentStatus NewPaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Gets or sets a unique hash code for an order. It is used to prevent duplicate orders from being created
    /// if an order is recovered using the customer's cart data when a payment webhook message is received.
    /// 0 (default) to not apply a unique hash code.
    /// </summary>
    public int PaymentReferenceHashCode { get; set; }
}