namespace Smartstore.Core.Checkout.Payment
{
    [Flags]
    public enum PaymentMethodType
    {
        Unknown = 0,

        /// <summary>
        /// The payment information is entered on the payment selection page during the checkout process.
        /// </summary>
        Standard = 1 << 0,

        /// <summary>
        /// After placing an order, the customer is redirected to a third-party site to complete the payment.
        /// </summary>
        /// <remarks>
        /// This type of payment is required for older payment methods. It is no longer recommended for new developments.
        /// </remarks>
        Redirection = 1 << 1,

        /// <summary>
        /// The payment provider initiates the payment process from the cart page, e.g. AmazonPay.
        /// </summary>
        Button = 1 << 2,

        /// <summary>
        /// The payment provider is capable of headless payments via UCP (Universal Commerce Protocol).
        /// </summary>
        UCP = 1 << 3
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
