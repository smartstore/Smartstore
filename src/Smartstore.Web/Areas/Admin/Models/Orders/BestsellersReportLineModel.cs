using Smartstore.Admin.Models.Catalog;
using Smartstore.Core.Common;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.SalesReport.Bestsellers.Fields.")]
    public class BestsellersReportLineModel : ProductOverviewModel
    {
        [LocalizedDisplay("*TotalAmount")]
        public Money TotalAmount { get; set; }

        [LocalizedDisplay("*TotalQuantity")]
        public string TotalQuantity { get; set; }
    }
}
