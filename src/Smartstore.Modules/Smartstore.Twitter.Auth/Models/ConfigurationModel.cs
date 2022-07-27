namespace Smartstore.Twitter.Auth.Models
{
    [LocalizedDisplay("Plugins.ExternalAuth.Twitter.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ConsumerKey")]
        public string ConsumerKey { get; set; }

        [LocalizedDisplay("*ConsumerSecret")]
        public string ConsumerSecret { get; set; }

        [LocalizedDisplay("*RedirectUri")]
        public string RedirectUrl { get; set; }
    }
}
