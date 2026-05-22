using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Orders;

[LocalizedDisplay("Admin.ReturnRequests.Fields.")]
public class ReturnCaseListModel : ModelBase
{
    [LocalizedDisplay("*ID")]
    [AdditionalMetadata("invariant", true)]
    public int? SearchId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [LocalizedDisplay("Common.Type")]
    public int? SearchReturnCaseKind { get; set; }

    [LocalizedDisplay("*Status")]
    public int? SearchStatusId { get; set; }

    [LocalizedDisplay("Admin.Orders.List.CustomerEmail")]
    public string CustomerEmail { get; set; }

    [LocalizedDisplay("Admin.Orders.List.CustomerName")]
    public string CustomerName { get; set; }

    [LocalizedDisplay("Admin.Orders.List.OrderNumber")]
    public string OrderNumber { get; set; }

    [UIHint("Stores")]
    [LocalizedDisplay("Admin.Common.Store.SearchFor")]
    public int SearchStoreId { get; set; }
}
