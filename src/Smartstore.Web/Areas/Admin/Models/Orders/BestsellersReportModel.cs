namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.SalesReport.Bestsellers.")]
    public class BestsellersReportModel : ModelBase
    {
        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*BillingCountry")]
        public int BillingCountryId { get; set; }

        [LocalizedDisplay("*OrderStatus")]
        public int OrderStatusId { get; set; }

        [LocalizedDisplay("*PaymentStatus")]
        public int PaymentStatusId { get; set; }

        [LocalizedDisplay("Admin.Customers.Reports.BestBy.ShippingStatus")]
        public int ShippingStatusId { get; set; }
    }
}
