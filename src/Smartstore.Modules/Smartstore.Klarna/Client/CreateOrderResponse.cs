using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CreateOrderResponse : KlarnaApiResponse
    {
        // Properties based on Klarna API response for creating an order
        // e.g., OrderId, RedirectUrl, FraudStatus etc.
        [JsonProperty("order_id")]
        public string OrderId { get; set; }

        [JsonProperty("redirect_url")]
        public string RedirectUrl { get; set; }

        [JsonProperty("fraud_status")]
        public string FraudStatus { get; set; }
    }
}
