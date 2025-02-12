namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.RecurringPayments.List.")]
    public class RecurringPaymentListModel : ModelBase
    {
        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*RemainingCycles")]
        public bool? RemainingCycles { get; set; }

        [LocalizedDisplay("*InitialOrderNumber")]
        public string InitialOrderNumber { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }
    }
}
