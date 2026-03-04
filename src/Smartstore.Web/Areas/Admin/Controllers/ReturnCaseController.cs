using System.Data;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class ReturnCaseController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;

        public ReturnCaseController(
            SmartDbContext db,
            ICurrencyService currencyService,
            ITaxService taxService,
            IOrderProcessingService orderProcessingService,
            IMessageFactory messageFactory,
            OrderSettings orderSettings)
        {
            _db = db;
            _currencyService = currencyService;
            _taxService = taxService;
            _orderProcessingService = orderProcessingService;
            _messageFactory = messageFactory;
            _orderSettings = orderSettings;
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
            var query = _db.ReturnCases
                .Include(x => x.Customer).ThenInclude(x => x.BillingAddress)
                .Include(x => x.Customer).ThenInclude(x => x.ShippingAddress)
                .AsNoTracking();

            if (model.SearchId.HasValue)
            {
                query = query.Where(x => x.Id == model.SearchId);
            }

            if (model.SearchStatusId.HasValue)
            {
                query = query.Where(x => x.ReturnCaseStatusId == model.SearchStatusId.Value);
            }

            if (model.CustomerEmail.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Customer.BillingAddress.Email, model.CustomerEmail);
            }

            if (model.CustomerName.HasValue())
            {
                query = query.Where(x =>
                    x.Customer.BillingAddress.LastName.Contains(model.CustomerName) ||
                    x.Customer.BillingAddress.FirstName.Contains(model.CustomerName));
            }

            if (model.OrderNumber.HasValue())
            {
                var orderQuery = int.TryParse(model.OrderNumber, out var orderId) && orderId != 0
                    ? _db.Orders.Where(x => x.OrderNumber.Contains(model.OrderNumber) || x.Id == orderId)
                    : _db.Orders.ApplySearchFilterFor(x => x.OrderNumber, model.OrderNumber);

                query =
                    from o in orderQuery
                    join oi in _db.OrderItems on o.Id equals oi.OrderId
                    join rr in query on oi.Id equals rr.OrderItemId
                    select rr;
            }

            var returnCases = await query
                .ApplyStandardFilter(null, null, model.SearchStoreId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var orderItemIds = returnCases.ToDistinctArray(x => x.OrderItemId);
            var orderItems = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .AsNoTracking()
                .Where(x => orderItemIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);

            var rows = returnCases.Select(x =>
            {
                var m = new ReturnCaseModel();
                PrepareReturnCaseRequestModel(m, x, orderItems.Get(x.OrderItemId), allStores, false, true);
                return m;
            })
            .ToList();

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
            await PrepareReturnCaseModelAsync(model, returnCase);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.ReturnCase.Update)]
        public async Task<IActionResult> Edit(ReturnCaseModel model, bool continueEditing)
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

                if (returnCase.RequestedAction != model.RequestedAction)
                {
                    returnCase.RequestedActionUpdatedOnUtc = utcNow;
                }

                returnCase.Quantity = model.Quantity;
                returnCase.ReasonForReturn = model.ReasonForReturn.EmptyNull();
                returnCase.RequestedAction = model.RequestedAction.EmptyNull();
                returnCase.CustomerComments = model.CustomerComments;
                returnCase.StaffNotes = model.StaffNotes;
                returnCase.AdminComment = model.AdminComment;
                returnCase.ReturnCaseStatusId = model.ReturnCaseStatusId;
                returnCase.UpdatedOnUtc = utcNow;

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditReturnCase, T("ActivityLog.EditReturnRequest"), returnCase.Id);
                NotifySuccess(T("Admin.ReturnRequests.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), returnCase.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareReturnCaseModelAsync(model, returnCase, true);

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
            NotifySuccess(T("Admin.ReturnRequests.Deleted"));

            return RedirectToAction(nameof(List));
        }

        private async Task<ReturnCaseModel> PrepareReturnCaseModelAsync(
            ReturnCaseModel model,
            ReturnCase returnCase,
            bool excludeProperties = false)
        {
            var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            var orderItem = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .FindByIdAsync(returnCase.OrderItemId);

            PrepareReturnCaseRequestModel(model, returnCase, orderItem, allStores, excludeProperties);

            return model;
        }

        private void PrepareReturnCaseRequestModel(
            ReturnCaseModel model,
            ReturnCase returnCase,
            OrderItem orderItem,
            Dictionary<int, Store> allStores,
            bool excludeProperties = false,
            bool forList = false)
        {
            Guard.NotNull(returnCase);

            var store = allStores.Get(returnCase.StoreId);
            var order = orderItem?.Order;
            var customer = returnCase.Customer;

            model.Id = returnCase.Id;
            model.ProductId = orderItem?.ProductId ?? 0;
            model.ProductSku = orderItem?.Sku?.NullEmpty() ?? orderItem?.Product?.Sku;
            model.ProductName = orderItem?.Product?.Name;
            model.ProductTypeName = orderItem?.Product?.GetProductTypeLabel(Services.Localization);
            model.ProductTypeLabelHint = orderItem?.Product?.ProductTypeLabelHint;
            model.AttributeInfo = orderItem?.AttributeDescription;
            model.OrderId = orderItem?.OrderId ?? 0;
            model.OrderNumber = order?.GetOrderNumber();
            model.CustomerId = returnCase.CustomerId;
            model.CustomerFullName = customer.GetFullName().NullEmpty() ?? customer.FindEmail().NaIfEmpty();
            model.CanSendEmailToCustomer = customer.FindEmail().HasValue();
            model.Quantity = returnCase.Quantity;
            model.ReturnCaseStatusString = Services.Localization.GetLocalizedEnum(returnCase.ReturnCaseStatus);
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(returnCase.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(returnCase.UpdatedOnUtc, DateTimeKind.Utc);
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
                    model.RequestedActionUpdated = Services.DateTimeHelper.ConvertToUserTime(returnCase.RequestedActionUpdatedOnUtc.Value, DateTimeKind.Utc);
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

                model.UpdateOrderItem = new UpdateOrderItemModel
                {
                    Id = returnCase.Id,
                    Caption = T("Admin.ReturnRequests.Accept.Caption"),
                    PostUrl = Url.Action("Accept", "ReturnCase"),
                };

                if (order != null)
                {
                    model.UpdateOrderItem.UpdateRewardPoints = order.RewardPointsWereAdded;
                    model.UpdateOrderItem.UpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
                    model.UpdateOrderItem.ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
                    model.UpdateOrderItem.ShowUpdateRewardPoints = order.OrderStatusId > (int)OrderStatus.Pending && order.RewardPointsWereAdded;
                }

                model.ReturnCaseInfo = TempData[UpdateOrderDetailsContext.InfoKey] as string;

                // The maximum amount that can be refunded for this return request.
                if (orderItem != null)
                {
                    var maxRefundAmount = Math.Max(orderItem.UnitPriceInclTax * returnCase.Quantity, 0);
                    if (maxRefundAmount > decimal.Zero)
                    {
                        model.MaxRefundAmount = new Money(
                            maxRefundAmount,
                            _currencyService.PrimaryCurrency,
                            false,
                            _taxService.GetTaxFormat(true, true));
                    }
                }
            }
        }
    }
}
