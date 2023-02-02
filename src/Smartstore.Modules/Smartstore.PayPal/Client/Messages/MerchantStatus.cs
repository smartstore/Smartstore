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

        /// <summary>
        /// Contains a list of PayPal products including state.
        /// </summary>
        [JsonProperty("products", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PayPalProducts[] Products;

        /// <summary>
        /// Contains a list of PayPal capabilties.
        /// </summary>
        [JsonProperty("capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PayPalCapabilities[] Capabilities;
    }

    public class PayPalProducts
    {
        /// <summary>
        /// The item name or title of the PayPal product.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;
        
        /// <summary>
        /// Vetting status 
        /// </summary>
        [JsonProperty("vetting_status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VettingStatus;

        /// <summary>
        /// Capabilities
        /// </summary>
        [JsonProperty("capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Capabilities;
    }

    public class PayPalCapabilities
    {
        /// <summary>
        /// The item name or title of the PayPal product.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;

        /// <summary>
        /// The curent status of the PayPal product (e.g. ACTIVE)
        /// </summary>
        [JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Status;

        /// <summary>
        /// Limits
        /// </summary>
        [JsonProperty("limits", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<PayPalLimits> Limits;
    }

    public class PayPalLimits
    {
        /// <summary>
        /// Type of the limit.
        /// </summary>
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type;
    }
}