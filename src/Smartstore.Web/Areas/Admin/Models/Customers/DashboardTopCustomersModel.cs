namespace Smartstore.Admin.Models.Customers
{
    public class DashboardTopCustomersModel : ModelBase
    {
        public IList<TopCustomerReportLineModel> TopCustomersByQuantity { get; set; }
        public IList<TopCustomerReportLineModel> TopCustomersByAmount { get; set; }
    }
}
