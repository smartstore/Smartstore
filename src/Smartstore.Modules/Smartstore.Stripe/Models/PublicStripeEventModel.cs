using Newtonsoft.Json;
using Smartstore.Web.Modelling;
using Stripe;

namespace Smartstore.StripeElements.Models
{
    public class PublicStripeEventModel : ModelBase
    {
        /// <summary>
        /// TODO
        /// </summary>
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

        // TODO: (mh) (core) Needed?
        //public StripeCustomer Customer { get; set; }
    }

    public class StripeCard
    {
        public string WalletName { get; set; }
    }
}