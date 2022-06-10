namespace Smartstore.PayPal.Models
{
    public class PublicPaymentMethodModel : ModelBase
    {
        public string Intent { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaymentSelection { get; set; } = false;

        public string ScriptUrl { get; set; }

        public string ButtonShape { get; set; }

        public string ButtonColor { get; set; }
    }
}