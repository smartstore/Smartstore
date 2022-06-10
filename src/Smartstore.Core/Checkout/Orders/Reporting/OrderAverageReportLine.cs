namespace Smartstore.Core.Checkout.Orders.Reporting
{
    /// <summary>
    /// Represents an order average report line.
    /// </summary>
    public partial class OrderAverageReportLine
    {
        // INFO: This remains as decimal type to allow easy calculation (e.g. in OrderQueryExtensions) without currency dependency.
        /// <summary>
        /// Gets or sets the order tax sum.
        /// </summary>
        public decimal SumTax { get; set; }

        // INFO: This remains as decimal type to allow easy calculation (e.g. in OrderQueryExtensions) without currency dependency.
        /// <summary>
        /// Gets or sets the order total sum.
        /// </summary>
        public decimal SumOrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the order count.
        /// </summary>
        public int OrderCount { get; set; }
    }
}