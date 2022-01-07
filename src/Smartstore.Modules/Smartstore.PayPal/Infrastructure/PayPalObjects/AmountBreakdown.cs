using Newtonsoft.Json;

namespace Smartstore.PayPal.Infrastructure.PayPalObjects
{
    public class AmountBreakdown
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("discount")]
        public Money Discount;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("handling")]
        public Money Handling;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("insurance")]
        public Money Insurance;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("item_total")]
        public Money ItemTotal;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("shipping")]
        public Money Shipping;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("shipping_discount")]
        public Money ShippingDiscount;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("tax_total")]
        public Money TaxTotal;
    }

    // TODO: (mh) (core) Rename and create own class.
    public class Money
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
