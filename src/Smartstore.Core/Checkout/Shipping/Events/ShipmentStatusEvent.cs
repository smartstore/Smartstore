namespace Smartstore.Core.Checkout.Shipping.Events
{
    /// <summary>
    /// Represents a shipment status event
    /// </summary>
    public partial class ShipmentStatusEvent
    {
        /// <summary>
        /// Gets or sets Event name
        /// </summary>
        public string EventName { get; set; }
        /// <summary>
        /// Gets or sets the location
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// Gets or sets the Two-letter country code
        /// </summary>
        public string CountryCode { get; set; }
        /// <summary>
        /// Gets or sets the date time
        /// </summary>
        public DateTime? Date { get; set; }
    }
}
