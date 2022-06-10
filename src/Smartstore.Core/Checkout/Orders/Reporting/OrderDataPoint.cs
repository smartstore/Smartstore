namespace Smartstore.Core.Checkout.Orders.Reporting
{
    /// <summary>
    /// Represents a order chart data point.
    /// </summary>
    public partial class OrderDataPoint
    {
        /// <summary>
        /// Gets or sets the date time at which the entity was created (utc).
        /// </summary>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the order total.
        /// </summary>
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the order status identifier.
        /// </summary>
        public int OrderStatusId { get; set; }

        /// <summary>
        /// Gets or sets the payment status identifier.
        /// </summary>
        public int PaymentStatusId { get; set; }

        /// <summary>
        /// Gets or sets the shipping status identifier.
        /// </summary>
        public int ShippingStatusId { get; set; }
    }
}