namespace Smartstore.PayPal.Client.Messages
{
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
