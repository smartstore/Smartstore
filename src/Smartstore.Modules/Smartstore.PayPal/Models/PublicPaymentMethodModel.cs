namespace Smartstore.PayPal.Models
{
    public class PublicPaymentMethodModel : ModelBase
    {
        public string Intent { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaymentSelection { get; set; } = false;

        /// <summary>
        /// Defines whether PayPal is the first payment method to be displayed on payment selection page.
        /// In this case we must handle visibility differently.
        /// </summary>
        public bool IsSelectedMethod { get; set; }

        public string ScriptUrl { get; set; }

        public string ButtonShape { get; set; }

        public string ButtonColor { get; set; }
    }
}