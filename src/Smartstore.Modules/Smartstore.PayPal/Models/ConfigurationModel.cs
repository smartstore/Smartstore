using Smartstore.Web.Modelling;

namespace Smartstore.PayPal.Models
{
    [LocalizedDisplay("Plugins.Smartstore.PayPal.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*UseSandbox")]
        public bool UseSandbox { get; set; }

        [LocalizedDisplay("*Account")]
        public string Account { get; set; }

        [LocalizedDisplay("*ClientId")]
        public string ClientId { get; set; }

        [LocalizedDisplay("*Secret")]
        public string Secret { get; set; }

        [LocalizedDisplay("*WebhookId")]
        public string WebhookId { get; set; }

        [LocalizedDisplay("*WebhookUrl")]
        public string WebhookUrl { get; set; }

        [LocalizedDisplay("*AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("*AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [LocalizedDisplay("*ShowButtonInMiniShoppingCart")]
        public bool ShowButtonInMiniShoppingCart { get; set; }

        [LocalizedDisplay("*DisabledFundings")]
        public string[] DisabledFundings { get; set; }
        
        [LocalizedDisplay("*EnabledFundings")]
        public string[] EnabledFundings { get; set; }

        [LocalizedDisplay("*Intent")]
        public string Intent { get; set; } = "authorize";

        [LocalizedDisplay("*ButtonShape")]
        public string ButtonShape { get; set; } = "pill";
 
        [LocalizedDisplay("*ButtonColor")]
        public string ButtonColor { get; set; } = "gold";
    }
}