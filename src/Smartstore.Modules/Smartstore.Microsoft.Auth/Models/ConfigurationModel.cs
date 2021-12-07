namespace Smartstore.Microsoft.Auth.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Microsoft.Auth.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ClientKeyIdentifier")]
        public string ClientKeyIdentifier { get; set; }

        [LocalizedDisplay("*ClientSecret")]
        public string ClientSecret { get; set; }

        [LocalizedDisplay("*RedirectUri")]
        public string RedirectUrl { get; set; }
    }
}
