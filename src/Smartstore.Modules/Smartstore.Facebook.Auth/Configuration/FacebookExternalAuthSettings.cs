using Smartstore.Core.Configuration;

namespace Smartstore.Facebook.Auth
{
    public class FacebookExternalAuthSettings : ISettings
    {
        public string ClientKeyIdentifier { get; set; }
        public string ClientSecret { get; set; }
    }
}
