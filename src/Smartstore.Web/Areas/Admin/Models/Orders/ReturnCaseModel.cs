using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore.Admin.Models.Orders;

[CustomModelPart]
[LocalizedDisplay("Admin.ReturnRequests.Fields.")]
public class ReturnCaseModel : TabbableModel
{
    [LocalizedDisplay("*ID")]
    public override int Id { get; set; }
    public int? WithdrawalId { get; set; }

    [LocalizedDisplay("Admin.Customers.Customers.Orders.Store")]
    public string StoreName { get; set; }

    [LocalizedDisplay("*Order")]
    public int OrderId { get; set; }
    public string OrderNumber { get; set; }

    [LocalizedDisplay("*Customer")]
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public bool CustomerDeleted { get; set; }

    [LocalizedDisplay("Admin.Orders.List.CustomerEmail")]
    public string CustomerEmail { get; set; }

    public int ProductId { get; set; }
    public string ProductSku { get; set; }
    public string AttributeInfo { get; set; }

    [LocalizedDisplay("*Product")]
    public string ProductName { get; set; }
    public string ProductTypeName { get; set; }
    public string ProductTypeLabelHint { get; set; }

    [LocalizedDisplay("Admin.ReturnRequests.MaxRefundAmount")]
    public Money MaxRefundAmount { get; set; } = Money.Zero;

    [LocalizedDisplay("*Quantity")]
    public int Quantity { get; set; }

    [LocalizedDisplay("*ReasonForReturn")]
    public string ReasonForReturn { get; set; }

    [LocalizedDisplay("*RequestedAction")]
    public string RequestedAction { get; set; }

    [LocalizedDisplay("*RequestedActionUpdatedOnUtc")]
    public DateTime? RequestedActionUpdated { get; set; }

    [LocalizedDisplay("*CustomerComments")]
    [UIHint("Textarea"), AdditionalMetadata("rows", 4)]
    public string CustomerComments { get; set; }

    [LocalizedDisplay("*StaffNotes")]
    [UIHint("Textarea"), AdditionalMetadata("rows", 4)]
    public string StaffNotes { get; set; }

    [LocalizedDisplay("Admin.Common.AdminComment")]
    [UIHint("Textarea"), AdditionalMetadata("rows", 4)]
    public string AdminComment { get; set; }

    [LocalizedDisplay("*Status")]
    public int ReturnCaseStatusId { get; set; }
    [LocalizedDisplay("*Status")]
    public string ReturnCaseStatusStr { get; set; }
    public string NextStep { get; set; }

    public string ReturnCaseStatusLabelClass
    {
        get
        {
            switch ((ReturnCaseStatus)ReturnCaseStatusId)
            {
                case ReturnCaseStatus.Pending:
                    return "fw-600";
                case ReturnCaseStatus.ItemsRepaired:
                case ReturnCaseStatus.ItemsRefunded:
                    return "text-success";
                case ReturnCaseStatus.RequestRejected:
                    return "text-danger";
                case ReturnCaseStatus.Cancelled:
                    return "muted";
                default:
                    return string.Empty;
            }                
        }
    }

    [LocalizedDisplay("Common.CreatedOn")]
    public DateTime CreatedOn { get; set; }

    [LocalizedDisplay("Common.UpdatedOn")]
    public DateTime UpdatedOn { get; set; }

    public bool CanAccept
        => Id != 0 && (ReturnCaseStatus)ReturnCaseStatusId < ReturnCaseStatus.ReturnAuthorized;

    public ReturnCaseKind Kind { get; set; }
    public string KindStr { get; set; }

    [NotMapped]
    public string KindLabelClass
    {
        get
        {
            return Kind switch
            {
                ReturnCaseKind.Return => "badge-ring badge-secondary",
                ReturnCaseKind.Withdrawal => "badge-outline badge-danger",
                _ => string.Empty,
            };
        }
    }

    public string ReturnCaseInfo { get; set; }
    public string EditUrl { get; set; }
    public string OrderEditUrl { get; set; }
    public string CustomerEditUrl { get; set; }
    public string ProductEditUrl { get; set; }

    [NotMapped, IgnoreDataMember]
    public UpdateOrderItemModel UpdateOrderItem { get; set; }
}
