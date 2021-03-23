using System.Collections.Generic;
using Smartstore.Core.Checkout.Orders.Reporting;

namespace Smartstore.Core.Checkout.Orders.Events
{
    public class CreatingBestsellersReportLineEvent
    {
        public CreatingBestsellersReportLineEvent()
        {
        }

        /// <summary>
        /// Best seller reports.
        /// </summary>
        public List<BestsellersReportLine> Reports { get; init; } = new();
    }
}