using Smartstore.Web.Modelling;

namespace Smartstore.PayPal.Models
{
    public class PublicPaymentMethodModel : ModelBase
    {
        public string Intent { get; set; }

        public decimal Amount { get; set; }

        public bool IsPaymentSelection { get; set; } = false;
    }
}