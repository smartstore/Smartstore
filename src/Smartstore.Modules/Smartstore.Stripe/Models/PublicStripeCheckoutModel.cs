using System.Collections.Generic;
using Newtonsoft.Json;
using Smartstore.Web.Modelling;

namespace Smartstore.StripeElements.Models
{
    public class PublicStripeElementsModel : ModelBase
    {
        /// <summary>
        /// Will be used to init Stripe JS SDK.
        /// </summary>
        public string PublicApiKey { get; set; }

        /// <summary>
        /// <see cref="StripePaymentRequest"/> serialized as JSON string.
        /// </summary>
        public string PaymentRequest { get; set; }

        /// <summary>
        /// ClientSecret for payment selection page.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Defines whether the component is rendered on payment selection page.
        /// </summary>
        public bool IsPaymentSelectionPage { get; set; }
    }

    /// <summary>
    /// Used to be serialized to JSON and be passed to Stripe JS SDK.
    /// </summary>
    public class StripePaymentRequest
    {
        /// <summary>
        /// The two-letter country code of your Stripe account (e.g., US).
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// Three character currency code (e.g., usd).
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// By default, the browser‘s payment interface only asks the customer for actual payment information. 
        /// A customer name can be collected by setting this option to true. This collected name will appears in the PaymentResponse object.
        /// 
        /// It's highly recommend you collect name as this also results in collection of billing address for Apple Pay.
        /// The billing address can be used to perform address verification and block fraudulent payments.
        /// For all other payment methods, the billing address is automatically collected when available.
        /// </summary>
        [JsonProperty("requestPayerName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool RequestPayerName { get; set; } = true;

        /// <summary>
        /// Same as RequestPayerName.
        /// </summary>
        [JsonProperty("requestPayerEmail", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool RequestPayerEmail { get; set; } = true;

        /// <summary>
        /// This PaymentItem is shown to the customer in the browser’s payment interface.
        /// </summary>
        [JsonProperty("total")]
        public StripePaymentItem Total { get; set; }

        /// <summary>
        /// These objects are shown as line items in the browser’s payment interface. 
        /// Note that the sum of the line item amounts does not need to add up to the total amount above.
        /// </summary>
        [JsonProperty("displayItems")]
        public List<StripePaymentItem> DisplayItems { get; set; } = new();
    }

    public class StripePaymentItem
    {
        /// <summary>
        /// A name that the browser shows the customer in the payment interface.
        /// </summary>
        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>
        /// The amount in the currency's subunit (e.g. cents, yen, etc.)
        /// </summary>
        [JsonProperty("amount")]
        public int Amount { get; set; }

        /// <summary>
        /// Allows to change thge amount afterwards.
        /// Should always be true in our usecase as the StripePaymentRequest will be created upon button usage.
        /// At this time we don't have the final amount and might update it later.
        /// </summary>
        [JsonProperty("pending")]
        public bool Pending { get; set; } = true;
    }
}