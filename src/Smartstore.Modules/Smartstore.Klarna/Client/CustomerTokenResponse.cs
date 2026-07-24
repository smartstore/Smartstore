using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CustomerTokenResponse : KlarnaApiResponse
    {
        [JsonProperty("token_id")]
        public string TokenId { get; set; }

        [JsonProperty("redirect_url")]
        public string RedirectUrl { get; set; }
    }
}
