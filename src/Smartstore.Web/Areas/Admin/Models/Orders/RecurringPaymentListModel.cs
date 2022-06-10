namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.RecurringPayments.List.")]
    public class RecurringPaymentListModel : ModelBase
    {
        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int StoreId { get; set; }

        [LocalizedDisplay("*CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("*CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("*InitialOrderNumber")]
        public string InitialOrderNumber { get; set; }

    }
}
