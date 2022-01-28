namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// The total order amount with an optional breakdown that provides details
    /// such as the total item amount, total tax amount, shipping, handling, insurance, and discounts, if any.
    /// If you specify `amount.breakdown`, 
    /// the amount equals `item_total` plus `tax_total` plus `shipping` plus `handling` plus `insurance` minus `shipping_discount` minus discount.
    /// </summary>
    public class AmountWithBreakdown
    {
        /// <summary>
        /// The breakdown of the amount. Breakdown provides details such as 
        /// total item amount, total tax amount, shipping, handling, insurance and discounts, if any.
        /// </summary>
        [JsonProperty("breakdown")]
        public AmountBreakdown AmountBreakdown;

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
