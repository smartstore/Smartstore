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
        public string MerchantId;

        /// <summary>
        /// The tracking id of the request.
        /// </summary>
        public string TrackingId;

        /// <summary>
        /// The legal name of the store.
        /// </summary>
        public string LegalName;

        /// <summary>
        /// Flag to indicate whether payments are receivable.
        /// </summary>
        public bool PaymentsReceivable;

        /// <summary>
        /// Flag to indicate whether the merchant email is confirmed.
        /// </summary>
        public bool PrimaryEmailConfirmed;

        /// <summary>
        /// Contains a list of PayPal products including state.
        /// </summary>
        public PayPalProducts[] Products;

        /// <summary>
        /// Contains a list of PayPal capabilties.
        /// </summary>
        public PayPalCapabilities[] Capabilities;
    }

    public class PayPalProducts
    {
        /// <summary>
        /// The item name or title of the PayPal product.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Vetting status 
        /// </summary>
        public string VettingStatus;

        /// <summary>
        /// Capabilities
        /// </summary>
        public List<string> Capabilities;
    }

    public class PayPalCapabilities
    {
        /// <summary>
        /// The item name or title of the PayPal product.
        /// </summary>
        public string Name;

        /// <summary>
        /// The curent status of the PayPal product (e.g. ACTIVE)
        /// </summary>
        public string Status;

        /// <summary>
        /// Limits
        /// </summary>
        public List<PayPalLimits> Limits;
    }

    public class PayPalLimits
    {
        /// <summary>
        /// Type of the limit.
        /// </summary>
        public string Type;
    }
}