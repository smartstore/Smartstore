namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a post process payment request.
    /// </summary>
    public partial class PostProcessPaymentRequest : PaymentRequest
    {
        /// <summary>
        /// A value indicating whether the customer has clicked the button to re-post the payment process on order detail page.
        /// Only applicable for payment type <see cref="PaymentMethodType.Redirection"/>.
        /// </summary>
        public bool IsRePostProcessPayment { get; set; }

        /// <summary>
        /// Gets or sets an URL to a third-party payment page.
        /// The customer is redirected to it to complete the payment after (!) the order has been placed.
        /// </summary>
        /// <remarks>
        /// This type of payment is required for older payment methods. It is recommended not to use it for new developments anymore.
        /// </remarks>
        public string RedirectUrl { get; set; }
    }
}
