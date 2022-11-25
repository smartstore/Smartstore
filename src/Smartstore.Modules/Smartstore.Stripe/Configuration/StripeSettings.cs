using Smartstore.Core.Configuration;

namespace Smartstore.StripeElements.Settings
{
    public class StripeSettings : ISettings
    {
        /// <summary>
        /// Specifies whether to use sandbox mode.
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Public Api key. Used to render Stripe Elements via JS-SDK.
        /// </summary>
        public string PublicApiKey { get; set; }

        /// <summary>
        /// Secret Api key. Used for server to server communication.
        /// </summary>
        public string SecrectApiKey { get; set; }

        /// <summary>
        /// Specifies whether the payment will be captured immediately or just authorized. 
        /// Possible values > automatic || manual
        /// </summary>
        public string CaptureMethod { get; set; } = "automatic";

        /// <summary>
        /// Specifies the additional handling fee charged to the customer when using this method.
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Specifies whether the addional fee should be a percentage value based on the current cart.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        /// <summary>
        /// Specifies whether to display the checkout button in offcanvas shopping cart.
        /// </summary>
        public bool ShowButtonInMiniShoppingCart { get; set; } = true;
    }
}