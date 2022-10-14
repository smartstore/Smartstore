namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures either a portion or the full authorized amount of an authorized payment.
    /// Smartstore doesn't support partial capturing. The only purpuse of this object is to transmit the invoice id.
    /// </summary>
    public class CaptureMessage
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MoneyMessage Amount;

        /// <summary>
        /// The date and time, in [Internet date and time format](https://tools.ietf.org/html/rfc3339#section-5.6). Seconds are required while fractional seconds are optional.<blockquote><strong>Note:</strong> The regular expression provides guidance but does not reject all invalid dates.</blockquote>
        /// </summary>
        [JsonProperty("create_time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CreateTime;

        /// <summary>
        /// The funds that are held on behalf of the merchant.
        /// </summary>
        [JsonProperty("disbursement_mode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DisbursementMode;

        /// <summary>
        /// Indicates whether you can make additional captures against the authorized payment. Set to `true` if you do not intend to capture additional payments against the authorization. Set to `false` if you intend to capture additional payments against the authorization.
        /// </summary>
        [JsonProperty("final_capture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? FinalCapture;

        /// <summary>
        /// The PayPal-generated ID for the captured payment.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id;

        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("invoice_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string InvoiceId;

        /// <summary>
        /// The status of the captured payment.
        /// </summary>
        [JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Status;

        /// <summary>
        /// The date and time, in [Internet date and time format](https://tools.ietf.org/html/rfc3339#section-5.6). Seconds are required while fractional seconds are optional.<blockquote><strong>Note:</strong> The regular expression provides guidance but does not reject all invalid dates.</blockquote>
        /// </summary>
        [JsonProperty("update_time", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UpdateTime;
    }
}
