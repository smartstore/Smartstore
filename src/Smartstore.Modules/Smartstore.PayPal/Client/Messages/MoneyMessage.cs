namespace Smartstore.PayPal.Client.Messages
{
    public class MoneyMessage
    {
        /// <summary>
        /// REQUIRED
        /// The three-character ISO-4217 currency code that identifies the currency.
        /// </summary>
        [JsonProperty("currency_code")]
        public string CurrencyCode;

        /// <summary>
        /// REQUIRED
        /// The value, which might be
        /// an integer for currencies like `JPY` that are not typically fractional. 
        /// Or a decimal fraction for currencies like `TND` that are subdivided into thousandths.
        /// </summary>
        [JsonProperty("value")]
        public string Value;
    }
}
