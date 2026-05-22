using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Orders;

public partial class ReturnCaseModel : EntityModelBase
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; }
    public int OrderItemId { get; set; }
    public ReturnCaseKind Kind { get; set; }
    public string KindStr { get; set; }
    public int Quantity { get; set; }
    public ReturnCaseStatus ReturnCaseStatus { get; set; }
    public string ReturnCaseStatusStr { get; set; }
    public string ReasonForReturn { get; set; }
    public string RequestedAction { get; set; }
    public string CustomerComments { get; set; }
    public DateTime CreatedOn { get; set; }

    public bool Complete
    {
        get
        {
            if (Kind == ReturnCaseKind.Withdrawal)
            {
                return false;
            }

            return ReturnCaseStatus == ReturnCaseStatus.ItemsRepaired
                || ReturnCaseStatus == ReturnCaseStatus.ItemsRefunded
                || ReturnCaseStatus == ReturnCaseStatus.RequestRejected
                || ReturnCaseStatus == ReturnCaseStatus.Cancelled;
        }
    }
}

internal class ReturnCaseMapper : IMapper<ReturnCase, ReturnCaseModel>
{
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly IWorkContext _workContext;

    public ReturnCaseMapper(IDateTimeHelper dateTimeHelper, IWorkContext workContext)
    {
        _dateTimeHelper = dateTimeHelper;
        _workContext = workContext;
    }

    public async Task MapAsync(ReturnCase from, ReturnCaseModel to, dynamic parameters = null)
    {
        Guard.NotNull(from);
        Guard.NotNull(to);

        var orderItem = parameters.OrderItem as OrderItem;
        var language = _workContext.WorkingLanguage;

        MiniMapper.Map(from, to);

        to.OrderId = orderItem?.OrderId ?? 0;
        to.OrderNumber = orderItem?.Order?.GetOrderNumber();
        to.KindStr = from.Kind.GetLocalizedEnum(language.Id);
        to.ReturnCaseStatusStr = from.ReturnCaseStatus.GetLocalizedEnum(language.Id);
        to.CreatedOn = _dateTimeHelper.ConvertToUserTime(from.CreatedOnUtc, DateTimeKind.Utc);
    }
}