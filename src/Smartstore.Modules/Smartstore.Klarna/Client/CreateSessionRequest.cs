using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CreateSessionRequest : KlarnaApiRequest
    {
        // Properties based on Klarna API for creating a session
        // e.g., OrderAmount, OrderLines, PurchaseCountry, Currency, Locale, MerchantUrls etc.
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

        [JsonProperty("merchant_urls")]
        public KlarnaMerchantUrls MerchantUrls { get; set; }
    }
}
