namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Orders.Fields.")]
    public class RecurringPaymentHistoryModel : OrderOverviewModel
    {
        public override int Id { get; set; }
        public int RecurringPaymentId { get; set; }

        [LocalizedDisplay("Admin.RecurringPayments.History.Order")]
        public int OrderId { get; set; }
        public string OrderEditUrl { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public new DateTime CreatedOn { get; set; }
    }
}
