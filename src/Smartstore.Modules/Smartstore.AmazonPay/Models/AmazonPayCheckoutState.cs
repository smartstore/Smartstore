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
}
