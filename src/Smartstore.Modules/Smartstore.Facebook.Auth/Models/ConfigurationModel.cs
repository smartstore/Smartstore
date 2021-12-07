namespace Smartstore.Facebook.Auth.Models
{
    [LocalizedDisplay("Plugins.ExternalAuth.Facebook.")]
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
