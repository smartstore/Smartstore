using Smartstore.Core.Common;

namespace Smartstore.AmazonPay.Models
{
    [Serializable]
    public class AmazonPayCheckoutState
    {
        public string OrderReferenceId { get; set; }
        public string AccessToken { get; set; }

        // Confirmation flow.
        public bool DetailsSet { get; set; }
        public bool IsConfirmed { get; set; }
        public string FormData { get; set; }
        public bool SubmitForm { get; set; }
    }

    public class CheckoutReviewResult
    {
        public bool Success { get; set; }
        public bool IsShippingRequired { get; set; }
    }

    public class CheckoutAdressResult
    {
        public bool Success { get; set; }
        public bool IsCountryAllowed { get; set; } = true;
        public string CountryCode { get; set; } = string.Empty;
        public Address Address { get; set; }
    }

    public class CheckoutConfirmResult
    {
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public List<string> Messages { get; set; } = new();
    }
}
