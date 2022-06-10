using Smartstore.Admin.Models.Catalog;

namespace Smartstore.Admin.Models.Orders
{
    // INFO: must be 'Admin.Catalog.Products.Fields.' for ProductOverviewModel.
    [LocalizedDisplay("Admin.Catalog.Products.Fields.")]
    public class BestsellersReportLineModel : ProductOverviewModel
    {
        [LocalizedDisplay("Admin.SalesReport.Bestsellers.Fields.TotalAmount")]
        public Money TotalAmount { get; set; }

        [LocalizedDisplay("Admin.SalesReport.Bestsellers.Fields.TotalQuantity")]
        public string TotalQuantity { get; set; }
    }
}
