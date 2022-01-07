using Smartstore.Core.Configuration;

namespace Smartstore.PayPal.Settings
{
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
        /// Specifies whether <see cref="AdditionalFee"/> is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        /// <summary>
        /// Specifies for additional fee charged to the customer when using this payment method.
        /// </summary>
        public decimal AdditionalFee { get; set; }

        /// <summary>
        /// Specifies which payment options should be disabled
        /// </summary>
        public string DisabledFundings { get; set; }

        /// <summary>
        /// Specifies which payment options should be enabled.
        /// </summary>
        public string EnabledFundings { get; set; }

        // TODO: (mh) (core) Make enum out of this.
        /// <summary>
        /// Specifies whether the payment will be captured immediately or just authorized.
        /// </summary>
        public string Intent { get; set; } = "authorize";
    }
}