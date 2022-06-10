namespace Smartstore.Admin.Models.Customers
{
    public class TopCustomersReportModel : ModelBase
    {
        [LocalizedDisplay("Admin.Customers.Reports.BestBy.StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.OrderStatus")]
        public int OrderStatusId { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.PaymentStatus")]
        public int PaymentStatusId { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.ShippingStatus")]
        public int ShippingStatusId { get; set; }
    }
}
