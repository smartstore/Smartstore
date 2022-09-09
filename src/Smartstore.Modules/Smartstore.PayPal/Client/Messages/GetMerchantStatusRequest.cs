using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets merchant status.
    /// </summary>
    public class GetMerchantStatusRequest : PayPalRequest<MerchantStatus>
    {
        public GetMerchantStatusRequest(string partnerId, string payerId)
            : base("/v1/customer/partners/{0}/merchant-integrations/{1}", HttpMethod.Get)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(partnerId), Uri.EscapeDataString(payerId));
            }
            catch (IOException)
            {
            }
            
            ContentType = "application/json";
            Headers.Add("PayPal-Partner-Attribution-Id", "Smartstore_Cart_PPCP");
        }

        public GetMerchantStatusRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }
    }

    // TODO: (mh) (core) Create own class file
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
