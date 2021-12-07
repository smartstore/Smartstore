namespace Smartstore.Twitter.Auth.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Twitter.Auth.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ConsumerKey")]
        public string ConsumerKey { get; set; }

        [LocalizedDisplay("*ConsumerSecret")]
        public string ConsumerSecret { get; set; }

        [LocalizedDisplay("*RedirectUrl")]
        public string RedirectUrl { get; set; }
    }
}
