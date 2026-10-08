using System.Data;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers;

public class ReturnCaseController : AdminController
{
    private readonly SmartDbContext _db;
    private readonly ITaxService _taxService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IMessageFactory _messageFactory;
    private readonly OrderSettings _orderSettings;
    private readonly Currency _primaryCurrency;

    public ReturnCaseController(
        SmartDbContext db,
        ITaxService taxService,
        ICurrencyService currencyService,
        IOrderProcessingService orderProcessingService,
        IMessageFactory messageFactory,
        OrderSettings orderSettings)
    {
        _db = db;
        _taxService = taxService;
        _orderProcessingService = orderProcessingService;
        _messageFactory = messageFactory;
        _orderSettings = orderSettings;
        _primaryCurrency = currencyService.PrimaryCurrency;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(List));
    }

    [Permission(Permissions.Order.ReturnCase.Read)]
    public IActionResult List()
    {
        ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();

        return View(new ReturnCaseListModel());
    }

    [Permission(Permissions.Order.ReturnCase.Read)]
    public async Task<IActionResult> ReturnCaseList(GridCommand command, ReturnCaseListModel model)
    {
        var dtHelper = Services.DateTimeHelper;
        DateTime? startDateUtc = model.StartDate == null
            ? null
            : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

        DateTime? endDateUtc = model.EndDate == null
            ? null
            : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

        var query = _db.ReturnCases
            .Include(x => x.Customer).ThenInclude(x => x.BillingAddress)
            .Include(x => x.Customer).ThenInclude(x => x.ShippingAddress)
            .AsNoTracking();

        if (model.SearchId.HasValue)
        {
            query = query.Where(x => x.Id == model.SearchId);
        }
        if (model.SearchReturnCaseKind.HasValue)
        {
            query = query.Where(x => x.Kind == (ReturnCaseKind)model.SearchReturnCaseKind.Value);
        }
        if (model.SearchStatusId.HasValue)
        {
            query = query.Where(x => x.ReturnCaseStatusId == model.SearchStatusId.Value);
        }
        if (model.CustomerEmail.HasValue())
        {
            query = query.ApplySearchFilter(
                model.CustomerEmail,
                LogicalRuleOperator.Or,
                x => x.Customer.Email,
                x => x.Customer.BillingAddress.Email);
        }
        if (model.CustomerName.HasValue())
        {
            query = query.ApplySearchFilter(
                model.CustomerName,
                LogicalRuleOperator.Or,
                x => x.Customer.FullName,
                x => x.Customer.BillingAddress.FirstName,
                x => x.Customer.BillingAddress.LastName);
        }
        if (model.OrderNumber.HasValue())
        {
            var orderQuery = int.TryParse(model.OrderNumber, out var orderId) && orderId != 0
                ? _db.Orders.Where(x => x.OrderNumber.Contains(model.OrderNumber) || x.Id == orderId)
                : _db.Orders.ApplySearchFilterFor(x => x.OrderNumber, model.OrderNumber);

            query =
                from o in orderQuery
                join oi in _db.OrderItems on o.Id equals oi.OrderId
                join rc in query on oi.Id equals rc.OrderItemId
                select rc;
        }

        var returnCases = await query
            .ApplyAuditDateFilter(startDateUtc, endDateUtc)
            .ApplyStandardFilter(null, null, model.SearchStoreId ?? 0)
            .ApplyGridCommand(command)
            .ToPagedList(command)
            .LoadAsync();

        var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
        var orderItemIds = returnCases.ToDistinctArray(x => x.OrderItemId);
        var orderItems = await _db.OrderItems
            .Include(x => x.Product)
            .Include(x => x.Order)
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x);
        var shipmentItems = (await _db.ShipmentItems
            .Include(x => x.Shipment)
            .AsNoTracking()
            .Where(x => orderItemIds.Contains(x.OrderItemId))
            .ToListAsync())
            .ToMultimap(x => x.OrderItemId, x => x);

        var rows = await returnCases
            .SelectAwait(async x =>
            {
                var m = new ReturnCaseModel();
                var shipments = shipmentItems.TryGetValues(x.OrderItemId, out var tmp) ? tmp : [];
                await PrepareReturnCaseModel(m, x, orderItems.Get(x.OrderItemId), shipments, allStores, false, true);
                return m;
            })
            .ToListAsync();

        return Json(new GridModel<ReturnCaseModel>
        {
            Rows = rows,
            Total = returnCases.TotalCount
        });
    }

    [Permission(Permissions.Order.ReturnCase.Read)]
    public async Task<IActionResult> Edit(int id)
    {
        var returnCase = await _db.ReturnCases
            .IncludeCustomer()
            .FindByIdAsync(id);
        if (returnCase == null)
        {
            return NotFound();
        }

        var model = new ReturnCaseModel();
        await PrepareReturnCaseModel(model, returnCase);

        return View(model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [FormValueRequired("save", "save-continue")]
    [Permission(Permissions.Order.ReturnCase.Update)]
    public async Task<IActionResult> Edit(ReturnCaseModel model, bool continueEditing, IFormCollection form)
    {
        var returnCase = await _db.ReturnCases
            .IncludeCustomer()
            .FindByIdAsync(model.Id);
        if (returnCase == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var utcNow = DateTime.UtcNow;

            model.ReasonForReturn = model.ReasonForReturn.NullEmpty();
            model.RequestedAction = model.RequestedAction.NullEmpty();

            if (returnCase.RequestedAction != model.RequestedAction)
            {
                returnCase.RequestedActionUpdatedOnUtc = utcNow;
            }

            returnCase.Quantity = model.Quantity;
            returnCase.ReasonForReturn = model.ReasonForReturn;
            returnCase.RequestedAction = model.RequestedAction;
            returnCase.CustomerComments = model.CustomerComments;
            returnCase.StaffNotes = model.StaffNotes;
            returnCase.AdminComment = model.AdminComment;
            returnCase.ReturnCaseStatusId = model.ReturnCaseStatusId;
            returnCase.UpdatedOnUtc = utcNow;

            await _db.SaveChangesAsync();

            await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, returnCase, form));
            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditReturnCase, T("ActivityLog.EditReturnRequest"), returnCase.Id);
            NotifySuccess(T("Admin.Common.SuccessfullySaved"));

            return continueEditing
                ? RedirectToAction(nameof(Edit), returnCase.Id)
                : RedirectToAction(nameof(List));
        }

        await PrepareReturnCaseModel(model, returnCase, true);

        return View(model);
    }

    [HttpPost, ActionName("Edit")]
    [FormValueRequired("notify-customer")]
    [Permission(Permissions.Order.ReturnCase.Update)]
    public async Task<IActionResult> NotifyCustomer(ReturnCaseModel model)
    {
        var returnCase = await _db.ReturnCases
            .IncludeCustomer()
            .FindByIdAsync(model.Id);

        var orderItem = await _db.OrderItems
            .Include(x => x.Product)
            .Include(x => x.Order)
            .FindByIdAsync(returnCase?.OrderItemId ?? 0);

        if (returnCase == null || orderItem == null)
        {
            return NotFound();
        }

        var msg = await _messageFactory.SendReturnCaseStatusChangedCustomerNotificationAsync(returnCase, orderItem, Services.WorkContext.WorkingLanguage.Id);
        if (msg?.Email?.Id != null)
        {
            NotifySuccess(T("Admin.ReturnRequests.Notified"));
        }

        return RedirectToAction(nameof(Edit), returnCase.Id);
    }

    [HttpPost]
    [Permission(Permissions.Order.ReturnCase.Accept)]
    public async Task<IActionResult> Accept(UpdateOrderItemModel model)
    {
        var returnCase = await _db.ReturnCases
            .IncludeCustomer()
            .FindByIdAsync(model.Id);

        var orderItem = await _db.OrderItems
            .Include(x => x.Order)
            .FindByIdAsync(returnCase?.OrderItemId ?? 0);

        if (returnCase == null || orderItem == null)
        {
            return NotFound();
        }

        var cancelQuantity = returnCase.Quantity > orderItem.Quantity ? orderItem.Quantity : returnCase.Quantity;

        var context = new UpdateOrderDetailsContext
        {
            OldQuantity = orderItem.Quantity,
            NewQuantity = Math.Max(orderItem.Quantity - cancelQuantity, 0),
            AdjustInventory = model.AdjustInventory,
            UpdateRewardPoints = model.UpdateRewardPoints,
            UpdateTotals = model.UpdateTotals
        };

        returnCase.ReturnCaseStatus = ReturnCaseStatus.ReturnAuthorized;

        // INFO: UpdateOrderDetailsAsync performs commit.
        await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

        Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), orderItem.Order.GetOrderNumber());

        TempData[UpdateOrderDetailsContext.InfoKey] = await InvokePartialViewAsync("OrderItemUpdateInfo", context);

        return RedirectToAction(nameof(Edit), new { id = returnCase.Id });
    }

    [HttpPost, ActionName("Edit")]
    [FormValueRequired("convert")]
    [Permission(Permissions.Order.ReturnCase.Update)]
    public async Task<IActionResult> Convert(int id)
    {
        var returnCase = await _db.ReturnCases.FindByIdAsync(id);
        if (returnCase == null)
        {
            return NotFound();
        }

        if (returnCase.Kind == ReturnCaseKind.Withdrawal)
        {
            returnCase.Kind = ReturnCaseKind.Return;
            returnCase.ReturnCaseStatus = ReturnCaseStatus.Pending;

            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditReturnCase, T("ActivityLog.EditReturnRequest"), returnCase.Id);
            NotifyInfo(T("ReturnCase.ConvertedWithdrawal", returnCase.ReturnCaseStatus.GetLocalizedEnum()));
        }

        return RedirectToAction(nameof(Edit), returnCase.Id);
    }

    [HttpPost]
    [Permission(Permissions.Order.ReturnCase.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var returnCase = await _db.ReturnCases.FindByIdAsync(id);
        if (returnCase == null)
        {
            return NotFound();
        }

        _db.ReturnCases.Remove(returnCase);
        await _db.SaveChangesAsync();

        Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteReturnCase, T("ActivityLog.DeleteReturnRequest"), id);
        NotifySuccess(T("Admin.Common.SuccessfullyDeleted"));

        return RedirectToAction(nameof(List));
    }

    private async Task<ReturnCaseModel> PrepareReturnCaseModel(
        ReturnCaseModel model,
        ReturnCase returnCase,
        bool excludeProperties = false)
    {
        var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
        var orderItem = await _db.OrderItems
            .Include(x => x.Product)
            .Include(x => x.Order)
            .FindByIdAsync(returnCase.OrderItemId, false);
        var shipmentItems = await _db.ShipmentItems
            .Include(x => x.Shipment)
            .AsNoTracking()
            .Where(x => x.OrderItemId == returnCase.OrderItemId)
            .ToListAsync();

        await PrepareReturnCaseModel(model, returnCase, orderItem, shipmentItems, allStores, excludeProperties);

        return model;
    }

    private async Task PrepareReturnCaseModel(
        ReturnCaseModel model,
        ReturnCase returnCase,
        OrderItem orderItem,
        IEnumerable<ShipmentItem> shipmentItems,
        Dictionary<int, Store> allStores,
        bool excludeProperties = false,
        bool forList = false)
    {
        Guard.NotNull(returnCase);

        var localization = Services.Localization;
        var dtHelper = Services.DateTimeHelper;
        var store = allStores.Get(returnCase.StoreId);
        var order = orderItem?.Order;
        var customer = returnCase.Customer;

        model.Id = returnCase.Id;
        model.WithdrawalId = returnCase.WithdrawalId;
        model.ProductId = orderItem?.ProductId ?? 0;
        model.ProductSku = orderItem?.Sku?.NullEmpty() ?? orderItem?.Product?.Sku;
        model.ProductName = orderItem?.Product?.Name;
        model.ProductTypeName = orderItem?.Product?.GetProductTypeLabel(localization);
        model.ProductTypeLabelHint = orderItem?.Product?.ProductTypeLabelHint;
        model.AttributeInfo = orderItem?.AttributeDescription;
        model.OrderId = orderItem?.OrderId ?? 0;
        model.OrderNumber = order?.GetOrderNumber();
        model.OrderCancelled = order?.OrderStatus == OrderStatus.Cancelled;
        model.OrderStatusStr = order != null ? localization.GetLocalizedEnum(order.OrderStatus) : string.Empty;
        model.CustomerId = returnCase.CustomerId;
        model.CustomerDeleted = customer.Deleted;
        model.CustomerEmail = customer.FindEmail();
        model.CustomerName = customer.GetDisplayName(T);
        model.Quantity = returnCase.Quantity;
        model.Kind = returnCase.Kind;
        model.KindStr = localization.GetLocalizedEnum(returnCase.Kind);
        model.ReturnCaseStatusStr = localization.GetLocalizedEnum(returnCase.ReturnCaseStatus);
        model.NextStep = returnCase.Kind == ReturnCaseKind.Return ? T($"ReturnCase.NextStep.{returnCase.ReturnCaseStatus}") : string.Empty;
        model.CreatedOn = dtHelper.ConvertToUserTime(returnCase.CreatedOnUtc, DateTimeKind.Utc);
        model.UpdatedOn = dtHelper.ConvertToUserTime(returnCase.UpdatedOnUtc, DateTimeKind.Utc);
        model.EditUrl = Url.Action(nameof(Edit), "ReturnCase", new { id = returnCase.Id });

        if (customer != null)
        {
            model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = returnCase.CustomerId });
        }

        if (orderItem != null)
        {
            model.OrderEditUrl = Url.Action("Edit", "Order", new { id = orderItem.OrderId });
            model.ProductEditUrl = Url.Action("Edit", "Product", new { id = orderItem.ProductId });
        }

        model.Shipments = shipmentItems
            .Where(x => x.Shipment != null)
            .Select(x => new ReturnCaseModel.ReturnCaseShipmentModel
            {
                Id = x.ShipmentId,
                Quantity = x.Quantity,
                ShippedOn = x.Shipment.ShippedDateUtc != null
                    ? dtHelper.ConvertToUserTime(x.Shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                    : null,
                DeliveredOn = x.Shipment.DeliveryDateUtc != null
                    ? dtHelper.ConvertToUserTime(x.Shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                    : null
            })
            .OrderBy(x => x.Id)
            .ToList();

        if (model.Shipments.Count > 0)
        {
            var shipmentLabel = T("Admin.Orders.Shipments.ID");
            var notShipped = T("Admin.Orders.Shipments.ShippedDate.NotYet");
            var notDelivered = T("Admin.Orders.Shipments.DeliveryDate.NotYet");

            model.ShippedOn = model.Shipments.Max(x => x.ShippedOn);
            model.ShippedShipmentCount = model.Shipments.Count(x => x.ShippedOn.HasValue);
            model.ShippedOnStr = model.ShippedOn?.ToString("g");

            if (model.ShippedShipmentCount > 1)
            {
                model.ShippedOnStr += '…';
            }

            model.ShippedOnSummary = string.Join(Environment.NewLine, 
                model.Shipments.Select(x => $"{shipmentLabel}{x.Id}: {x.ShippedOn?.ToString("g") ?? notShipped}"));

            model.DeliveredOn = model.Shipments.Max(x => x.DeliveredOn);
            model.DeliveredShipmentCount = model.Shipments.Count(x => x.DeliveredOn.HasValue);
            model.DeliveredOnStr = model.DeliveredOn?.ToString("g");

            if (model.DeliveredShipmentCount > 1)
            {
                model.DeliveredOnStr += '…';
            }

            model.DeliveredOnSummary = string.Join(Environment.NewLine, 
                model.Shipments.Select(x => $"{shipmentLabel}{x.Id}: {x.DeliveredOn?.ToString("g") ?? notDelivered}"));
        }

        if (allStores.Count > 1)
        {
            model.StoreName = store?.Name;
        }

        if (!excludeProperties)
        {
            model.ReasonForReturn = returnCase.ReasonForReturn;
            model.RequestedAction = returnCase.RequestedAction;

            if (returnCase.RequestedActionUpdatedOnUtc.HasValue)
            {
                model.RequestedActionUpdated = dtHelper.ConvertToUserTime(returnCase.RequestedActionUpdatedOnUtc.Value, DateTimeKind.Utc);
            }

            model.CustomerComments = returnCase.CustomerComments;
            model.StaffNotes = returnCase.StaffNotes;
            model.AdminComment = returnCase.AdminComment;
            model.ReturnCaseStatusId = returnCase.ReturnCaseStatusId;
        }

        if (!forList)
        {
            string returnReasons = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestReasons, order?.CustomerLanguageId, store?.Id, true, false);
            string returnActions = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestActions, order?.CustomerLanguageId, store?.Id, true, false);
            string unspec = T("Common.Unspecified");

            var reasonForReturn = returnReasons.SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x, Selected = x == returnCase.ReasonForReturn })
                .ToList();
            reasonForReturn.Insert(0, new SelectListItem { Text = unspec, Value = string.Empty });

            var actionsForReturn = returnActions.SplitSafe(',')
                .Select(x => new SelectListItem { Text = x, Value = x, Selected = x == returnCase.RequestedAction })
                .ToList();
            actionsForReturn.Insert(0, new SelectListItem { Text = unspec, Value = string.Empty });

            ViewBag.ReasonForReturn = reasonForReturn;
            ViewBag.ActionsForReturn = actionsForReturn;

            model.UpdateOrderItem = new()
            {
                Id = returnCase.Id,
                Caption = T("Admin.ReturnRequests.Accept.Caption"),
                PostUrl = Url.Action(nameof(Accept), "ReturnCase"),
            };

            if (order != null)
            {
                model.UpdateOrderItem.UpdateRewardPoints = order.RewardPointsWereAdded;
                model.UpdateOrderItem.UpdateTotals = false;
                model.UpdateOrderItem.ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
                model.UpdateOrderItem.ShowUpdateRewardPoints = order.OrderStatusId > (int)OrderStatus.Pending && order.RewardPointsWereAdded;
            }

            model.ReturnCaseInfo = TempData[UpdateOrderDetailsContext.InfoKey] as string;

            if (orderItem != null)
            {
                // The maximum amount that can be refunded for this return request.
                var maxRefundAmount = Math.Max(orderItem.UnitPriceInclTax * returnCase.Quantity, 0);
                if (maxRefundAmount > decimal.Zero)
                {
                    model.MaxRefundAmount = new(maxRefundAmount, _primaryCurrency, false, _taxService.GetTaxFormat(true, true));
                }
            }

            ViewBag.ReturnCaseStatuses = Enum.GetValues<ReturnCaseStatus>()
                .Select(x => new ExtendedSelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = localization.GetLocalizedEnum(x),
                    Selected = x == returnCase.ReturnCaseStatus,
                    CustomProperties = new() { ["NextStep"] = T($"ReturnCase.NextStep.{x}").Value }
                })
                .ToList();
        }
    }
}