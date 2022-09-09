namespace Smartstore.PayPal.Models
{
    [LocalizedDisplay("Plugins.Smartstore.PayPal.")]
    public class ConfigurationModel : ModelBase
    {
        public bool HasCredentials { get; set; } = false;
        public bool PaymentsReceivable { get; set; } = false;
        public bool PrimaryEmailConfirmed { get; set; } = false;
        public bool WebHookCreated { get; set; } = false;

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