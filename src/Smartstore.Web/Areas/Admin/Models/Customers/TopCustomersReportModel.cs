namespace Smartstore.Admin.Models.Customers
{
    public class TopCustomersReportModel : ModelBase
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.OrderStatus")]
        public int OrderStatusId { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.PaymentStatus")]
        public int PaymentStatusId { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.ShippingStatus")]
        public int ShippingStatusId { get; set; }
    }
}
