using Smartstore.Web.Modelling;

namespace Smartstore.Google.Auth.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Google.Auth.")]
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
