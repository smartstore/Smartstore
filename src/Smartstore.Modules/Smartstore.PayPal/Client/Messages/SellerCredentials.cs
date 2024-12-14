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
        public string ClientId;

        /// <summary>
        /// The client secret of the merchant.
        /// </summary>
        public string ClientSecret;

        /// <summary>
        /// The payer id of the merchant.
        /// </summary>
        public string PayerId;
    }
}
