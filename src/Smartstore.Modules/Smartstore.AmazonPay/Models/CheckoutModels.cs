using Smartstore.Core.Common;

namespace Smartstore.AmazonPay.Models
{
    public class AmazonPayCheckoutState
    {
        /// <summary>
        /// The identifier of the AmazonPay checkout session object.
        /// </summary>
        public string SessionId { get; set; }

        // Confirmation flow.
        public bool IsConfirmed { get; set; }
        public string FormData { get; set; }

        /// <summary>
        /// Order is confimed by buyer and AmazonPay -> automatically submit confirm form.
        /// </summary>
        public bool SubmitForm { get; set; }
    }

    public class CheckoutReviewResult
    {
        public bool Success { get; set; }
        public bool IsShippingMethodMissing { get; set; }
        public bool RequiresAddressUpdate { get; set; }
    }

    public class CheckoutAdressResult
    {
        public bool Success { get; set; }
        public bool IsCountryAllowed { get; set; } = true;
        public string CountryCode { get; set; } = string.Empty;
        public Address Address { get; set; }
    }
}
