using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CreateOrderRequest : KlarnaApiRequest
    {
        // Properties based on Klarna API for creating an order
        // Often similar to CreateSessionRequest but might include authorization_token
        [JsonProperty("purchase_country")]
        public string PurchaseCountry { get; set; }

        [JsonProperty("purchase_currency")]
        public string PurchaseCurrency { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("order_amount")]
        public long OrderAmount { get; set; }

        [JsonProperty("order_tax_amount")]
        public long OrderTaxAmount { get; set; }

        [JsonProperty("order_lines")]
        public KlarnaOrderLine[] OrderLines { get; set; }

        // Billing and shipping addresses, customer details etc.
    }
}
