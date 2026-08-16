using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class UpdateSessionRequest : KlarnaApiRequest
    {
        // Properties for updating a session
        // e.g., OrderAmount, OrderLines etc. (similar to CreateSessionRequest)
        [JsonProperty("order_amount")]
        public long OrderAmount { get; set; }

        [JsonProperty("order_lines")]
        public KlarnaOrderLine[] OrderLines { get; set; }
    }
}
