using Newtonsoft.Json;

namespace Smartstore.Klarna.Client
{
    public class KlarnaOrderLine
    {
        [JsonProperty("type")]
        public string Type { get; set; } // e.g. "physical", "digital", "shipping_fee"

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("unit_price")]
        public long UnitPrice { get; set; } // In cents

        [JsonProperty("total_amount")]
        public long TotalAmount { get; set; } // In cents

        [JsonProperty("tax_rate")]
        public long TaxRate { get; set; } // In percent * 100, e.g., 2500 for 25%

        [JsonProperty("total_tax_amount")]
        public long TotalTaxAmount { get; set; } // In cents
    }
}
