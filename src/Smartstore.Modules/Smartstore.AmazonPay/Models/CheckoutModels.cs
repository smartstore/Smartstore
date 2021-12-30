using Smartstore.Core.Common;

namespace Smartstore.AmazonPay.Models
{
    [Serializable]
    public class AmazonPayCheckoutState
    {
        public static string Key => "AmazonPayCheckoutState";

        /// <summary>
        /// The identifier of the AmazonPay checkout session object.
        /// </summary>
        public string CheckoutSessionId { get; set; }
        //public string OrderReferenceId { get; set; }
        //public string AccessToken { get; set; }

        // Confirmation flow.
        //public bool DetailsSet { get; set; }
        public bool IsConfirmed { get; set; }
        public string FormData { get; set; }

        /// <summary>
        /// Order is confimed by buyer and AmazonPay -> automatically submit confirm form.
        /// </summary>
        public bool SubmitForm { get; set; }
    }

    [Serializable]
    public class AmazonPayCheckoutCompleteInfo
    {
        public static string Key => "AmazonPayCheckoutCompleteInfo";

        public string Note { get; set; }
        public bool UseWidget { get; set; }
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
