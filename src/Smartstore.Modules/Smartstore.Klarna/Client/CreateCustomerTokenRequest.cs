using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class CreateCustomerTokenRequest : KlarnaApiRequest
    {
        // Properties based on Klarna API for generating a customer token
        [JsonProperty("purchase_country")]
        public string PurchaseCountry { get; set; }

        [JsonProperty("purchase_currency")]
        public string PurchaseCurrency { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("intended_use")] // e.g. SUBSCRIPTION
        public string IntendedUse { get; set; }

        [JsonProperty("merchant_urls")]
        public KlarnaMerchantUrls MerchantUrls { get; set; }
    }
}
