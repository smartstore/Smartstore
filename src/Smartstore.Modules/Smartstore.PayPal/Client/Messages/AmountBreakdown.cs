namespace Smartstore.PayPal.Client.Messages
{
    public class AmountBreakdown
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("discount")]
        public MoneyMessage Discount;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("handling")]
        public MoneyMessage Handling;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("insurance")]
        public MoneyMessage Insurance;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("item_total")]
        public MoneyMessage ItemTotal;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("shipping")]
        public MoneyMessage Shipping;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("shipping_discount")]
        public MoneyMessage ShippingDiscount;

        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("tax_total")]
        public MoneyMessage TaxTotal;
    }
}
