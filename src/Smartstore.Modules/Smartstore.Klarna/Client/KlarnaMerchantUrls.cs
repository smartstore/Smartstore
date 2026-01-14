using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class KlarnaMerchantUrls
    {
        [JsonProperty("confirmation")]
        public string Confirmation { get; set; }

        [JsonProperty("notification")]
        public string Notification { get; set; }

        [JsonProperty("push")]
        public string Push { get; set; }
    }
}
