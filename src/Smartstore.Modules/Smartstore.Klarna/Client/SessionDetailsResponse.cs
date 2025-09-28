using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class SessionDetailsResponse : KlarnaApiResponse
    {
        // Properties for session details
        // e.g., Status, OrderAmount, OrderLines, ClientToken, ExpiresAt etc.
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("client_token")]
        public string ClientToken { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("order_amount")]
        public long OrderAmount { get; set; }

        [JsonProperty("order_lines")]
        public KlarnaOrderLine[] OrderLines { get; set; }
    }
}
