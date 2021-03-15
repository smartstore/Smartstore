namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a post process payment request.
    /// </summary>
    public partial class PostProcessPaymentRequest : PaymentRequest
    {
        /// <summary>
        /// A value indicating whether the customer has clicked the button to re-post the payment process.
        /// </summary>
        public bool IsRePostProcessPayment { get; set; }

        /// <summary>
        /// Gets or sets an URL to a payment page of the payment provider to fulfill the payment.
        /// </summary>
        public string RedirectUrl { get; set; }
    }
}
