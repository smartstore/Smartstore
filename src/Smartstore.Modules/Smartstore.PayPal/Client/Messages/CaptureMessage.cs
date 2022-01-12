using Newtonsoft.Json;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Captures either a portion or the full authorized amount of an authorized payment.
    /// Smartstore doesn't support partional capturing. The only purpuse of this object is to transmit the invoice id.
    /// </summary>
    public class CaptureMessage
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [JsonProperty("amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MoneyMessage Amount;

        /// <summary>
        /// Indicates whether you can make additional captures against the authorized payment. Set to `true` if you do not intend to capture additional payments against the authorization. Set to `false` if you intend to capture additional payments against the authorization.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? FinalCapture;

        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("invoice_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string InvoiceId;
    }
}
