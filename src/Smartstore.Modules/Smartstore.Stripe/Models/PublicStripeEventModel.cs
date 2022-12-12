using Newtonsoft.Json;
using Smartstore.Web.Modelling;

namespace Smartstore.StripeElements.Models
{
    /// <summary>
    /// Event object which is returned by stripe when terminal has accepted the payment.
    /// </summary>
    public class PublicStripeEventModel : ModelBase
    {
        public string MethodName { get; set; }

        public string PayerName { get; set; }

        public string PayerEmail { get; set; }

        public string PayerPhone { get; set; }

        public StripePaymentMethod PaymentMethod { get; set; }
        
        public string WalletName { get; set; }
    }

    public class StripePaymentMethod
    {
        public string Id { get; set; }

        [JsonProperty("billing_details")]
        public ChargeBillingDetails BillingDetails { get; set; }
        public StripeCard Card { get; set; }
        public string Created { get; set; }
        public bool LiveMode { get; set; }
        public string Object { get; set; }
        public string Type { get; set; }

        // INFO: (mh) (core) Implement in future
        //public StripeCustomer Customer { get; set; }
    }

    public class StripeCard
    {
        public string WalletName { get; set; }
    }
}