using Smartstore.Core.Configuration;

namespace Smartstore.Google.Auth
{
    public class GoogleExternalAuthSettings : ISettings
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
    }
}
