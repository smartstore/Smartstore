namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Merchant status.
    /// </summary>
    public class MerchantStatus
    {
        /// <summary>
        /// The client id of the merchant.
        /// </summary>
        [JsonProperty("merchant_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MerchantId;

        /// <summary>
        /// The tracking id of the request.
        /// </summary>
        [JsonProperty("tracking_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TrackingId;

        /// <summary>
        /// The legal name of the store.
        /// </summary>
        [JsonProperty("legal_name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LegalName;

        /// <summary>
        /// Flag to indicate whether payments are receivable.
        /// </summary>
        [JsonProperty("payments_receivable", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PaymentsReceivable;

        /// <summary>
        /// Flag to indicate whether the merchant email is confirmed.
        /// </summary>
        [JsonProperty("primary_email_confirmed", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PrimaryEmailConfirmed;

        // TODO: (mh) (core) 
        // products
        // capabilities
    }
}