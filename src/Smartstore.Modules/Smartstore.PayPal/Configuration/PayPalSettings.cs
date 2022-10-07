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
        public bool UseSandbox { get; set; } = false;

        /// <summary>
        /// Specifies whether to display the checkout button in mini shopping cart.
        /// </summary>
        public bool ShowButtonInMiniShoppingCart { get; set; } = true;

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
        /// Specifies which payment options should be disabled.
        /// </summary>
        public string DisabledFundings { get; set; }

        /// <summary>
        /// Specifies which payment options should be enabled.
        /// </summary>
        public string EnabledFundings { get; set; }

        /// <summary>
        /// Specifies whether to display the pay later widget on product detail pages.
        /// </summary>
        public bool DisplayProductDetailPayLaterWidget { get; set; } = false;

        /// <summary>
        /// Specifies whether the payment will be captured immediately or just authorized.
        /// </summary>
        public PayPalTransactionType Intent { get; set; } = PayPalTransactionType.Authorize;

        /// <summary>
        /// Specifies the form of the button.
        /// </summary>
        public string ButtonShape { get; set; }

        /// <summary>
        /// Specifies the color of the button.
        /// </summary>
        public string ButtonColor { get; set; }
    }
}