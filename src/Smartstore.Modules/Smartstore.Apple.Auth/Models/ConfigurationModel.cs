using System.ComponentModel.DataAnnotations;

namespace Smartstore.Apple.Auth.Models
{
    [LocalizedDisplay("Plugins.Smartstore.Apple.Auth.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*ClientId")]
        public string ClientId { get; set; }

        [LocalizedDisplay("*TeamId")]
        public string TeamId { get; set; }

        [LocalizedDisplay("*KeyId")]
        public string KeyId { get; set; }

        [UIHint("TextArea")]
        [LocalizedDisplay("*PrivateKey")]
        public string PrivateKey { get; set; }

        [LocalizedDisplay("*RedirectUri")]
        public string RedirectUrl { get; set; }
    }
}

