using System.Collections.Generic;
using Newtonsoft.Json;

namespace Smartstore.PayPal.Infrastructure.PayPalObjects
{
    public class BillingPlan
    {
        /// <summary>
        /// REQUIRED
        /// The plan name.
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        // TODO: (mh) (core) Is there a way to set MaxLength to 127 via attribute
        /// <summary>
        /// REQUIRED
        /// The plan description. Maximum length is 127 single-byte alphanumeric characters.
        /// </summary>
        [JsonProperty("description")]
        public string Description;

        /// <summary>
        /// REQUIRED
        /// The plan type. Indicates whether the payment definitions in the plan have a fixed number of or infinite payment cycles. 
        /// Possible values are:
        /// FIXED:       The plan has a fixed number of payment cycles.
        /// INFINITE:    The plan has infinite, or 0, payment cycles.
        /// </summary>
        [JsonProperty("type")]
        public string Type;

        /// <summary>
        /// A payment definition, which determines how often and for how long the customer is charged. 
        /// Includes the interval at which the customer is charged, the charge amount, and optional shipping fees and taxes.
        /// </summary>
        [JsonProperty("payment_definitions")]
        public List<PaymentDefinition> PaymentDefinitions = new();
    }
}
