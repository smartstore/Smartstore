namespace Smartstore.PayPal.Models
{
    public class ApplePayPaymentRequest : ModelBase
    {
        public string CountryCode { get; set; }

        public string CurrencyCode { get; set; }

        public string TotalLabel { get; set; }

        public string TotalAmount { get; set; }

        public bool RequiresShipping { get; set; }
    }
}
