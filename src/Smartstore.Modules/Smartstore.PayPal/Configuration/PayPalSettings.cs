using Smartstore.Core.Configuration;

namespace Smartstore.PayPal.Settings
{
    public enum PayPalTransactionType
    {
        Authorize = 1,
        Capture = 2
    }

    public class PayPalSettings : ISettings
    {
        /// <summary>
        /// Specifies whether to use sandbox mode.
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// PayPal account
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// PayPal app client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// PayPal app secret
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// PayPal payer id
        /// </summary>
        public string PayerId { get; set; }

        /// <summary>
        /// PayPal merchant name
        /// </summary>
        public string MerchantName { get; set; }

        /// <summary>
        /// PayPal webhook id
        /// </summary>
        public string WebhookId { get; set; }

        /// <summary>
        /// Specifies whether to display the pay later widget on product detail pages.
        /// </summary>
        public bool DisplayProductDetailPayLaterWidget { get; set; }

        /// <summary>
        /// Specifies whether the payment will be captured immediately or just authorized.
        /// </summary>
        public PayPalTransactionType Intent { get; set; } = PayPalTransactionType.Capture;

        /// <summary>
        /// Specifies the form of the buttons.
        /// </summary>
        public string ButtonShape { get; set; } = "rect";

        /// <summary>
        /// Specifies the color of the buttons.
        /// </summary>
        public string ButtonColor { get; set; }

        /// <summary>
        /// Specifies a text that is displayed by PayPal as service instruction (mandatory for pay upon invoice). 
        /// </summary>
        [LocalizedProperty]
        public string CustomerServiceInstructions { get; set; }

        /// <summary>
        /// Specifies which payment options should be displayed in OffCanvasCart.
        /// </summary>
        public string FundingsOffCanvasCart { get; set; } = "paypal";

        /// <summary>
        /// Specifies which payment options should be displayed on cart page.
        /// </summary>
        public string FundingsCart { get; set; } = "paypal";

        /// <summary>
        /// Specifies the limit to which the Pay Upon Invoice should be offered.
        /// </summary>
        public decimal PayUponInvoiceLimit { get; set; } = 2500;

        /// <summary>
        /// Specifies whether to transmit tracking numbers to PayPal.
        /// </summary>
        public bool TransmitTrackingNumbers { get; set; }
    }
}