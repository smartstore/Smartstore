using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    public class Subscription
    {
        /// <summary>
        /// The ID of the plan.
        /// </summary>
        [Required]
        [MaxLength(26)]
        public string PlanId;

        /// <summary>
        /// The quantity of the product in the subscription.
        /// </summary>
        [MaxLength(32)]
        public string Quantity;

        /// <summary>
        /// DEPRECATED. Indicates whether the subscription auto-renews after the billing cycles complete.
        /// </summary>
        //public bool AutoRenewal = false;

        /// <summary>
        /// The quantity of the product in the subscription.
        /// </summary>
        [MaxLength(127)]
        public string CustomId;

        /// <summary>
        /// The date and time when the subscription started, in Internet date and time format.
        /// Default: "Current time"
        /// </summary>
        public string StartTime;

        /// <summary>
        /// The shipping charges.
        /// </summary>
        public MoneyMessage ShippingAmount { get; set; }

        /// <summary>
        /// The subscriber request information.
        /// </summary>
        public Subscriber Subscriber { get; set; }

        /// <summary>
        /// The application context, which customizes the payer experience during the subscription approval process with PayPal.
        /// </summary>
        [JsonProperty("application_context")]
        public PayPalApplictionContext AppContext;

        /// <summary>
        /// An inline plan object to customise the subscription. 
        /// You can override plan level default attributes by providing customised values for the subscription in this object.
        /// </summary>
        public Plan Plan;
    }

    public class Subscriber
    {
        /// <summary>
        /// The email address of the payer.
        /// </summary>
        public string EmailAddress;

        /// <summary>
        /// The name of the payer. Supports only the given_name and surname properties.
        /// </summary>
        public NameMessage Name;

        /// <summary>
        /// The phone number of the customer. 
        /// Available only when you enable the Contact Telephone Number option in the Profile & Settings for the merchant's PayPal account. 
        /// The phone.phone_number supports only national_number.
        /// </summary>
        public Phone Phone;

        /// <summary>
        /// The shipping details.
        /// </summary>
        public ShippingDetail Shipping;
    }

    public class Phone
    {
        public PhoneMessage PhoneMessage;
    }
}
