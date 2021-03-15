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
        /// All payment information is entered on the site.
        /// </summary>
        Standard = 10,

        /// <summary>
        /// A customer is redirected to a third-party site in order to complete the payment.
        /// </summary>
        Redirection = 15,

        /// <summary>
        /// Payment via button on cart page.
        /// </summary>
        Button = 20,

        /// <summary>
        /// All payment information is entered on the site and is available via button.
        /// </summary>
        StandardAndButton = 25,

        /// <summary>
        /// Payment information is entered in checkout and customer is redirected to complete payment (e.g. 3D Secure) after order has been placed.
        /// </summary>
        StandardAndRedirection = 30
    }

    /// <summary>
    /// Represents a payment status.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Pending.
        /// </summary>
        Pending = 10,

        /// <summary>
        /// Authorized.
        /// </summary>
        Authorized = 20,

        /// <summary>
        /// Paid.
        /// </summary>
        Paid = 30,

        /// <summary>
        /// Partially Refunded.
        /// </summary>
        PartiallyRefunded = 35,

        /// <summary>
        /// Refunded.
        /// </summary>
        Refunded = 40,

        /// <summary>
        /// Voided.
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
    /// The reason for the automatic recording of the payment amount.
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
        OrderDelivered
    }
}
