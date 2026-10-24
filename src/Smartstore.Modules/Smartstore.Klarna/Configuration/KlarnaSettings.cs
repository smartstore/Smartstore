using Smartstore.Core.Configuration;

namespace Smartstore.Klarna.Configuration
{
    public class KlarnaSettings : ISettings
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public bool UseSandbox { get; set; }
        // Add any other settings Klarna might require, e.g., Region specific endpoints
        public string Region { get; set; } // e.g., "EU", "NA", "OC"
    }
}
