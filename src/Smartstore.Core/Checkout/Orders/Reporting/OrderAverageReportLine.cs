using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders.Reporting
{
    /// <summary>
    /// Represents an order average report line.
    /// </summary>
    public partial class OrderAverageReportLine
    {
        /// <summary>
        /// Gets or sets the tax summary.
        /// </summary>
        public Money SumTax { get; set; }

        /// <summary>
        /// Gets or sets the order total summary.
        /// </summary>
        public Money SumOrders { get; set; }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int CountOrders { get; set; }
    }
}