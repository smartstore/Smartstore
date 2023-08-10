namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a payment method type.
    /// </summary>
    public enum PaymentMethodType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// All payment information is entered on the payment selection page.
        /// </summary>
        Standard = 10,

        /// <summary>
        /// A customer is redirected to a third-party site to complete the payment after (!) the order has been placed.
        /// </summary>
        /// <remarks>
        /// This type of payment is required for older payment methods. It is recommended not to use it for new developments anymore.
        /// </remarks>
        Redirection = 15,

        /// <summary>
        /// Payment via button on cart page.
        /// </summary>
        Button = 20,

        /// <summary>
        /// All payment information is entered on the payment selection page and is available via button on cart page.
        /// </summary>
        StandardAndButton = 25,

        /// <summary>
        /// Payment information is entered in checkout and customer is redirected to complete payment (e.g. 3D Secure)
        /// after the order has been placed.
        /// </summary>
        StandardAndRedirection = 30
    }

    /// <summary>
    /// Represents a payment status.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// The initial payment status if no further status information is available yet.
        /// </summary>
        Pending = 10,

        /// <summary>
        /// The payment has been authorized (but not captured) by the payment provider.
        /// Usually this means that the payment amount is reserved for later capturing.
        /// </summary>
        Authorized = 20,

        /// <summary>
        /// The payment has been captured against the payment gateway.
        /// It does not necessarily mean that the paid amount has been credited to the merchant's account.
        /// </summary>
        Paid = 30,

        /// <summary>
        /// The paid amount has been partially refunded.
        /// </summary>
        PartiallyRefunded = 35,

        /// <summary>
        /// The paid amount has been fully refunded.
        /// </summary>
        Refunded = 40,

        /// <summary>
        /// The payment has been cancelled.
        /// </summary>
        Voided = 50,
    }

    /// <summary>
    /// Represents a recurring payment type.
    /// </summary>
    public enum RecurringPaymentType
    {
        /// <summary>
        /// Not supported.
        /// </summary>
        NotSupported = 0,

        /// <summary>
        /// Manual.
        /// </summary>
        Manual = 10,

        /// <summary>
        /// Automatic (payment is processed on payment gateway site).
        /// </summary>
        Automatic = 20
    }

    /// <summary>
    /// The reason for the automatic capturing of the payment amount.
    /// </summary>
    public enum CapturePaymentReason
    {
        /// <summary>
        /// Capture payment because the order has been marked as shipped.
        /// </summary>
        OrderShipped = 0,

        /// <summary>
        /// Capture payment because the order has been marked as delivered.
        /// </summary>
        OrderDelivered,

        /// <summary>
        /// Capture payment because the order has been marked as completed.
        /// </summary>
        OrderCompleted
    }
}
