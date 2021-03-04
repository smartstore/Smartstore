namespace Smartstore.Core.Checkout.Orders.Reporting
{
    /// <summary>
    /// Represents an order average report line.
    /// </summary>
    public partial class OrderAverageReportLine
    {
        // (money) This remains as a decimal number to allow easy calculation (e.g. in OrderQueryExtensions) without currency dependency.
        /// <summary>
        /// Gets or sets the tax summary.
        /// </summary>
        public decimal SumTax { get; set; }

        // (money) This remains as a decimal number to allow easy calculation (e.g. in OrderQueryExtensions) without currency dependency.
        /// <summary>
        /// Gets or sets the order total summary.
        /// </summary>
        public decimal SumOrders { get; set; }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int CountOrders { get; set; }
    }
}