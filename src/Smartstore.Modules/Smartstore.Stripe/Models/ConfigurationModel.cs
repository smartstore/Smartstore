using Smartstore.Web.Modelling;

namespace Smartstore.StripeElements.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Stripe.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*PublicApiKey")]
        public string PublicApiKey { get; set; }

        [LocalizedDisplay("*SecrectApiKey")]
        public string SecrectApiKey { get; set; }

        [LocalizedDisplay("*WebhookSecret")]
        public string WebhookSecret { get; set; }

        [LocalizedDisplay("*WebhookUrl")]
        public string WebhookUrl { get; set; }

        [LocalizedDisplay("*CaptureMethod")]
        public string CaptureMethod { get; set; }

        [LocalizedDisplay("*AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("*AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [LocalizedDisplay("*ShowButtonInMiniShoppingCart")]
        public bool ShowButtonInMiniShoppingCart { get; set; }
    }
}