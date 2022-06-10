using Smartstore.Core.Configuration;

namespace Smartstore.Microsoft.Auth
{
    public class MicrosoftExternalAuthSettings : ISettings
    {
        public string ClientKeyIdentifier { get; set; }
        public string ClientSecret { get; set; }
    }
}
