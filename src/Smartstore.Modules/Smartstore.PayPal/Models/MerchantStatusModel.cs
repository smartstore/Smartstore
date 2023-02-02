using Smartstore.PayPal.Client.Messages;

namespace Smartstore.PayPal.Models
{
    [LocalizedDisplay("Plugins.Smartstore.PayPal.")]
    public class MerchantStatusModel
    {
        /// <summary>
        /// Flag to indicate PayPal should be configured before checking status.
        /// </summary>
        public bool DisplayOnboardingHint { get; set; }

        /// <summary>
        /// Raw response of the request
        /// </summary>
        public string RawResponse { get; set; }

        /// <summary>
        /// Flag to indicate whether a webhook was created.
        /// </summary>
        public bool WebHookCreated { get; set; }

        /// <summary>
        /// The MerchantId a.k.a. PayerId
        /// </summary>
        [LocalizedDisplay("*PayerId")]
        public string MerchantId { get; set; }

        /// <summary>
        /// The tracking id of the request.
        /// </summary>
        [LocalizedDisplay("*Account")]
        public string TrackingId { get; set; }

        /// <summary>
        /// The legal name of the account.
        /// </summary>
        [LocalizedDisplay("*MerchantName")]
        public string LegalName { get; set; }

        /// <summary>
        /// Flag to indicate whether payments are receivable.
        /// </summary>
        public bool PaymentsReceivable { get; set; }

        /// <summary>
        /// Flag to indicate whether the merchant email is confirmed.
        /// </summary>
        public bool PrimaryEmailConfirmed { get; set; }

        /// <summary>
        /// Contains a list of PayPal products including state.
        /// </summary>
        public PayPalProducts[] Products { get; set; }

        /// <summary>
        /// Contains a list of PayPal capabilties.
        /// </summary>
        public PayPalCapabilities[] Capabilities { get; set; }
    }
}