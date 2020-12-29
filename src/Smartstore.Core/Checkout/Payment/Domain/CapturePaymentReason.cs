namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// The reason for the automatic recording of the payment amount
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