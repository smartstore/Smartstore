using Smartstore.Core.Configuration;

namespace Smartstore.Core.Checkout.Payment
{
    public class PaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the system names of active payment methods
        /// </summary>
        public List<string> ActivePaymentMethodSystemNames { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether customers are allowed to repost (complete) payments for redirection payment methods
        /// </summary>
        public bool AllowRePostingPayments { get; set; } = true;

        /// <summary>
        /// Gets or sets the reason for automatic payment capturing
        /// </summary>
        public CapturePaymentReason? CapturePaymentReason { get; set; }

        /// <summary>
        /// Gets or sets the system names of payment methods that should be displayed on product detail pages.
        /// </summary>
        public string[] ProductDetailPaymentMethodSystemNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether small payment icons should be used on payment selection page.
        /// </summary>
        public bool DisplayPaymentMethodIcons { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip the payment selection during checkout, if only one payment method is available.
        /// </summary>
        public bool SkipPaymentSelectionIfSingleOption { get; set; }
    }
}