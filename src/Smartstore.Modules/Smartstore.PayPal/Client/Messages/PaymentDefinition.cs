namespace Smartstore.PayPal.Client.Messages
{
    public class PaymentDefinition
    {
        /// <summary>
        /// REQUIRED
        /// The payment definition name.
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// REQUIRED
        /// The payment definition type. Each plan must have at least one regular payment definition and, optionally, a trial payment definition. 
        /// Each definition specifies how often and for how long the customer is charged.
        /// Possible values are: TRIAL,REGULAR.
        /// </summary>
        [JsonProperty("type")]
        public string Type = "REGULAR";

        /// <summary>
        /// REQUIRED
        /// The interval at which the customer is charged. Value cannot be greater than 12 months.
        /// </summary>
        [JsonProperty("frequency_interval")]
        public string FrequencyInterval;

        /// <summary>
        /// REQUIRED
        /// The frequency of the payment in this definition.
        /// Possible values: WEEK,DAY,YEAR,MONTH.
        /// </summary>
        [JsonProperty("frequency")]
        public string Frequency;

        /// <summary>
        /// REQUIRED
        /// The number of payment cycles. For infinite plans with a regular payment definition, set cycles to 0.
        /// </summary>
        [JsonProperty("cycles")]
        public string Cycles;

        /// <summary>
        /// REQUIRED
        /// The currency and amount of the charge to make at the end of each payment cycle for this definition.
        /// </summary>
        [JsonProperty("amount")]
        public MoneyMessage Amount;
    }
}