using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Core.Checkout.Payment
{
    /// <summary>
    /// Represents a post process payment request.
    /// </summary>
    public partial class PostProcessPaymentRequest : PaymentRequest
    {
        /// <summary>
        /// Whether the customer has clicked the button to re-post the payment process.
        /// </summary>
        public bool IsRePostProcessPayment { get; set; }

        /// <summary>
        /// URL to a payment provider to fulfill the payment. The .NET core will redirect to it.
        /// </summary>
        public string RedirectUrl { get; set; }
    }
}
