using Smartstore.Core.Configuration;

namespace Smartstore.Core.Checkout.Shipping
{
    public class ShippingSettings : ISettings
    {
        /// <summary>
        /// Gets or sets system names of active shipping rate computation methods
        /// </summary>
        public List<string> ActiveShippingRateComputationMethodSystemNames { get; set; } = new() { "Shipping.FixedRate" };
        /// <summary>
        /// Gets or sets a value indicating whether 'Free shipping over X' is enabled
        /// </summary>
        public bool FreeShippingOverXEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value of 'Free shipping over X' option
        /// </summary>
        public decimal FreeShippingOverXValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 'Free shipping over X' option
        /// should be evaluated over 'X' value including tax or not
        /// </summary>
        public bool FreeShippingOverXIncludingTax { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether 'Estimate shipping' option is enabled
        /// </summary>
        public bool EstimateShippingEnabled { get; set; } = true;

        /// <summary>
        /// A value indicating whether customers should see shipment events on their order details pages
        /// </summary>
        public bool DisplayShipmentEventsToCustomers { get; set; }

        /// <summary>
        /// Gets or sets shipping origin address
        /// </summary>
        public int ShippingOriginAddressId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should return valid options if there are any (no matter of the errors returned by other shipping rate compuation methods).
        /// </summary>
        public bool ReturnValidOptionsIfThereAreAny { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to skip the shipping selection during checkout, if only one shipping method is available.
        /// </summary>
        public bool SkipShippingIfSingleOption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to charge only the highest shipping surcharge of products
        /// </summary>
        public bool ChargeOnlyHighestProductShippingSurcharge { get; set; }

        /// <summary>
        /// Specifies a custom formatting pattern for delivery time dates (e.g. "M").
        /// </summary>
        public string DeliveryTimesDateFormat { get; set; }

        /// <summary>
        /// Specifies the hour (value between 1 and 24) by which the order will be shipped the same day.
        /// </summary>
        public int? TodayShipmentHour { get; set; } = 14;

        /// <summary>
        /// Specifies whether delivery takes place only on workweek days.
        /// </summary>
        public bool DeliveryOnWorkweekDaysOnly { get; set; } = true;
    }
}