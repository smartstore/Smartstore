namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.SalesReport.NeverSold.")]
    public class NeverSoldReportModel : ModelBase
    {
        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }
    }
}
