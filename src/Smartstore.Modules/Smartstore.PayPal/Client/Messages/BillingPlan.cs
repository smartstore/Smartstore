using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    public class BillingPlan
    {
        /// <summary>
        /// REQUIRED.
        /// The plan name.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Name;

        /// <summary>
        /// REQUIRED.
        /// The plan description. Maximum length is 127 single-byte alphanumeric characters.
        /// </summary>
        [MaxLength(127)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Description;

        /// <summary>
        /// REQUIRED.
        /// The plan type. Indicates whether the payment definitions in the plan have a fixed number of or infinite payment cycles. 
        /// Possible values are:
        /// FIXED:       The plan has a fixed number of payment cycles.
        /// INFINITE:    The plan has infinite, or 0, payment cycles.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Type;

        /// <summary>
        /// A payment definition, which determines how often and for how long the customer is charged. 
        /// Includes the interval at which the customer is charged, the charge amount, and optional shipping fees and taxes.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public List<PaymentDefinition> PaymentDefinitions = [];
    }
}
