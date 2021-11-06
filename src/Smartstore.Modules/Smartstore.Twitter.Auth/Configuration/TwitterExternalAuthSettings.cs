using Smartstore.Core.Configuration;

namespace Smartstore.Twitter.Auth
{
    public class TwitterExternalAuthSettings : ISettings
    {
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
    }
}
