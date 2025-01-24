namespace Smartstore.Admin.Models.Orders
{
    public class NeverSoldReportModel : ModelBase
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
