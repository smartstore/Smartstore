using Smartstore.Core.Common;

namespace Smartstore.Core.Checkout.Orders.Reporting
{
    /// <summary>
    /// Represents a best customer report line.
    /// </summary>
    public partial class TopCustomerReportLine
    {
        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Gets or sets the order total.
        /// </summary>
        public Money OrderTotal { get; set; }

        /// <summary>
        /// Gets or sets the order count.
        /// </summary>
        public int OrderCount { get; set; }
    }
}