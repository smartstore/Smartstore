using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Smartstore.PayPal.Client.Messages
{
    public class Plan
    {
        /// <summary>
        /// The PayPal plan id obtained in the creation response.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The ID of the product created through Catalog Products API.
        /// </summary>
        [Required]
        [MaxLength(22)]
        public string ProductId { get; set; }

        /// <summary>
        /// The plan name.
        /// </summary>
        [Required]
        [MaxLength(22)]
        public string Name { get; set; }

        /// <summary>
        /// The initial state of the plan. Allowed input values are CREATED and ACTIVE.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public PlanStatus Status { get; set; } = PlanStatus.ACTIVE;

        /// <summary>
        /// The detailed description of the plan.
        /// </summary>
        [MaxLength(127)]
        public string Description { get; set; }

        /// <summary>
        /// An array of billing cycles for trial billing and regular billing.
        /// </summary>
        [Required]
        public List<BillingCycle> BillingCycles { get; set; }

        /// <summary>
        /// Indicates whether you can subscribe to this plan by providing a quantity for the goods or service.
        /// </summary>
        public bool QuantitySupported { get; set; } = false;

        /// <summary>
        /// The payment preferences for a subscription.
        /// </summary>
        [Required]
        public PaymentPreferences PaymentPreferences { get; set; }

        /// <summary>
        /// The tax details.
        /// </summary>
        public Taxes Taxes { get; set; }
    }

    /// <summary>
    /// The status of the plan.
    /// </summary>
    public enum PlanStatus
    {
        /// <summary>
        /// The plan was created. You cannot create subscriptions for a plan in this state.
        /// </summary>
        CREATED,
        /// <summary>
        /// The plan is inactive.
        /// </summary>
        INACTIVE,
        /// <summary>
        /// The plan is active. You can only create subscriptions for a plan in this state.
        /// </summary>
        ACTIVE
    }

    public class BillingCycle
    {
        /// <summary>
        /// The tenure type of the billing cycle.
        /// In case of a plan having trial cycle, only 2 trial cycles are allowed per plan.
        /// </summary>
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public TenureType TenureType { get; set; }

        /// <summary>
        /// The order in which this cycle is to run among other billing cycles.
        /// For example, a trial billing cycle has a sequence of 1 while a regular billing cycle has a sequence of 2, so that trial cycle runs before the regular cycle.
        /// </summary>
        [Required]
        [Range(1, 99)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public int Sequence { get; set; }

        /// <summary>
        /// The number of times this billing cycle gets executed.
        /// Trial billing cycles can only be executed a finite number of times (value between 1 and 999 for total_cycles). 
        /// Regular billing cycles can be executed infinite times (value of 0 for total_cycles) or a finite number of times (value between 1 and 999 for total_cycles).
        /// </summary>
        [Range(1, 999)]
        public int? TotalCycles { get; set; } = 1;

        /// <summary>
        /// The active pricing scheme for this billing cycle. A free trial billing cycle does not require a pricing scheme.
        /// </summary>
        public PricingScheme PricingScheme { get; set; }

        /// <summary>
        /// The frequency details for this billing cycle.
        /// </summary>
        [Required]
        public Frequency Frequency { get; set; }
    }

    public class PricingScheme
    {
        /// <summary>
        /// The pricing model for tiered plan. The tiers parameter is required.
        /// </summary>
        public PricingModel PricingModel { get; set; }

        /// <summary>
        /// An array of pricing tiers which are used for billing volume/tiered plans. pricing_model field has to be specified.
        /// </summary>
        public List<PricingTier> Tiers { get; set; }

        /// <summary>
        /// The fixed amount to charge for the subscription. The changes to fixed amount are applicable to both existing and future subscriptions. 
        /// For existing subscriptions, payments within 10 days of price change are not affected.
        /// </summary>
        public MoneyMessage FixedPrice { get; set; }
    }

    public enum PricingModel
    {
        VOLUME,
        TIERED
    }

    public class PricingTier
    {
        /// <summary>
        /// The starting quantity for the tier.
        /// </summary>
        [MaxLength(32)]
        [Required]
        public string StartingQuantity { get; set; }

        /// <summary>
        /// The ending quantity for the tier.
        /// </summary>
        public string EndingQuantity { get; set; }

        /// <summary>
        /// The pricing amount for the tier.
        /// </summary>
        [Required]
        public MoneyMessage Amount { get; set; }
    }

    /// <summary>
    /// The tenure type of the billing cycle.
    /// </summary>
    public enum TenureType
    {
        /// <summary>
        /// A regular billing cycle.
        /// </summary>
        REGULAR,
        /// <summary>
        /// A trial billing cycle.
        /// </summary>
        TRIAL
    }

    public class Frequency
    {
        /// <summary>
        /// The interval at which the subscription is charged or billed.
        /// </summary>
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public IntervalUnit IntervalUnit { get; set; }

        /// <summary>
        /// The number of intervals after which a subscriber is billed.
        /// For example, if the interval_unit is DAY with an interval_count of 2, the subscription is billed once every two days. 
        /// </summary>
        [Range(1, 365)]
        public int? IntervalCount { get; set; } = 1;
    }

    public enum IntervalUnit
    {
        [EnumMember(Value = "DAY")]
        Day,
        [EnumMember(Value = "WEEK")]
        Week,
        [EnumMember(Value = "MONTH")]
        Month,
        [EnumMember(Value = "YEAR")]
        Year
    }

    public class PaymentPreferences
    {
        /// <summary>
        /// Indicates whether to automatically bill the outstanding amount in the next billing cycle.
        /// </summary>
        public bool AutoBillOutstanding { get; set; } = true;

        /// <summary>
        /// The action to take on the subscription if the initial payment for the setup fails.
        /// </summary>
        [MaxLength(24)]
        public SetupFeeFailureAction SetupFeeFailureAction { get; set; } = SetupFeeFailureAction.CANCEL;

        /// <summary>
        /// The maximum number of payment failures before a subscription is suspended.
        /// </summary>
        [Range(1, 999)]
        public int PaymentFailureThreshold { get; set; } = 0;

        /// <summary>
        /// The initial set-up fee for the service.
        /// </summary>
        public MoneyMessage SetupFee { get; set; }
    }

    public enum SetupFeeFailureAction
    {
        CONTINUE,
        CANCEL
    }

    public class Taxes
    {
        /// <summary>
        /// Indicates whether the tax was already included in the billing amount.
        /// </summary>
        public bool Inclusive { get; set; } = true;

        /// <summary>
        /// The tax percentage on the billing amount.
        /// </summary>
        [Required]
        public string Percentage { get; set; }
    }
}