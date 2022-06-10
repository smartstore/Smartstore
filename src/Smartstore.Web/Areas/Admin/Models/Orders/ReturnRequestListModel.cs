namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.ReturnRequests.Fields.")]
    public class ReturnRequestListModel : ModelBase
    {
        [LocalizedDisplay("*ID")]
        public int? SearchId { get; set; }

        [LocalizedDisplay("*Status")]
        public int? SearchReturnRequestStatusId { get; set; }

        [LocalizedDisplay("Admin.Common.Store.SearchFor")]
        public int SearchStoreId { get; set; }

        [LocalizedDisplay("Admin.Orders.List.CustomerEmail")]
        public string CustomerEmail { get; set; }

        [LocalizedDisplay("Admin.Orders.List.CustomerName")]
        public string CustomerName { get; set; }

        [LocalizedDisplay("Admin.Orders.List.OrderNumber")]
        public string OrderNumber { get; set; }
    }
}
