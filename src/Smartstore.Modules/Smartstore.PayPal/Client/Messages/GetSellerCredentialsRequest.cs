using System.Net.Http.Headers;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Gets seller credentials.
    /// </summary>
    public class GetSellerCredentialsRequest : PayPalRequest<SellerCredentials>
    {
        public GetSellerCredentialsRequest(string partnerId, string token)
            : base("/v1/customer/partners/{0}/merchant-integrations/credentials", HttpMethod.Get)
        {
            try
            {
                Path = Path.FormatInvariant(Uri.EscapeDataString(partnerId));
            }
            catch (IOException)
            {
            }
            
            ContentType = "application/json";
            
            Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Headers.Add("PayPal-Partner-Attribution-Id", "Smartstore_Cart_PPCP");
        }

        public GetSellerCredentialsRequest WithRequestId(string payPalRequestId)
        {
            Headers.Add("PayPal-Request-Id", payPalRequestId);
            return this;
        }
    }

    // TODO: (mh) (core) Create own class file
    /// <summary>
    /// Seller credentials.
    /// </summary>
    public class SellerCredentials
    {
        /// <summary>
        /// The client id of the merchant.
        /// </summary>
        [JsonProperty("client_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientId;

        /// <summary>
        /// The client secret of the merchant.
        /// </summary>
        [JsonProperty("client_secret", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ClientSecret;

        /// <summary>
        /// The payer id of the merchant.
        /// </summary>
        [JsonProperty("payer_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PayerId;
    }
}
