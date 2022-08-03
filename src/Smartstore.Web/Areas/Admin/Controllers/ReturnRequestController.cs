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
    public class ReturnRequestController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IMessageFactory _messageFactory;
        private readonly OrderSettings _orderSettings;

        public ReturnRequestController(
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

        [Permission(Permissions.Order.ReturnRequest.Read)]
        public IActionResult List()
        {
            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();

            return View(new ReturnRequestListModel());
        }

        [Permission(Permissions.Order.ReturnRequest.Read)]
        public async Task<IActionResult> ReturnRequestList(GridCommand command, ReturnRequestListModel model)
        {
            var query = _db.ReturnRequests
                .Include(x => x.Customer).ThenInclude(x => x.BillingAddress)
                .Include(x => x.Customer).ThenInclude(x => x.ShippingAddress)
                .AsNoTracking();

            if (model.SearchId.HasValue)
            {
                query = query.Where(x => x.Id == model.SearchId);
            }

            if (model.SearchReturnRequestStatusId.HasValue)
            {
                query = query.Where(x => x.ReturnRequestStatusId == model.SearchReturnRequestStatusId.Value);
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

            var returnRequests = await query
                .ApplyStandardFilter(null, null, model.SearchStoreId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var orderItemIds = returnRequests.ToDistinctArray(x => x.OrderItemId);
            var orderItems = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .AsNoTracking()
                .Where(x => orderItemIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);

            var rows = await returnRequests.SelectAwait(async x =>
            {
                var m = new ReturnRequestModel();
                await PrepareReturnRequestModel(m, x, orderItems.Get(x.OrderItemId), allStores, false, true);
                return m;
            })
            .AsyncToList();

            return Json(new GridModel<ReturnRequestModel>
            {
                Rows = rows,
                Total = returnRequests.TotalCount
            });
        }

        [Permission(Permissions.Order.ReturnRequest.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var returnRequest = await _db.ReturnRequests
                .IncludeCustomer()
                .FindByIdAsync(id);

            if (returnRequest == null)
            {
                return NotFound();
            }

            var model = new ReturnRequestModel();
            await PrepareReturnRequestModel(model, returnRequest);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.ReturnRequest.Update)]
        public async Task<IActionResult> Edit(ReturnRequestModel model, bool continueEditing)
        {
            var returnRequest = await _db.ReturnRequests
                .IncludeCustomer()
                .FindByIdAsync(model.Id);

            if (returnRequest == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var utcNow = DateTime.UtcNow;

                if (returnRequest.RequestedAction != model.RequestedAction)
                {
                    returnRequest.RequestedActionUpdatedOnUtc = utcNow;
                }

                returnRequest.Quantity = model.Quantity;
                returnRequest.ReasonForReturn = model.ReasonForReturn.EmptyNull();
                returnRequest.RequestedAction = model.RequestedAction.EmptyNull();
                returnRequest.CustomerComments = model.CustomerComments;
                returnRequest.StaffNotes = model.StaffNotes;
                returnRequest.AdminComment = model.AdminComment;
                returnRequest.ReturnRequestStatusId = model.ReturnRequestStatusId;
                returnRequest.UpdatedOnUtc = utcNow;

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditReturnRequest, T("ActivityLog.EditReturnRequest"), returnRequest.Id);
                NotifySuccess(T("Admin.ReturnRequests.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), returnRequest.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareReturnRequestModel(model, returnRequest, true);

            return View(model);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("notify-customer")]
        [Permission(Permissions.Order.ReturnRequest.Update)]
        public async Task<IActionResult> NotifyCustomer(ReturnRequestModel model)
        {
            var returnRequest = await _db.ReturnRequests
                .IncludeCustomer()
                .FindByIdAsync(model.Id);

            var orderItem = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .FindByIdAsync(returnRequest?.OrderItemId ?? 0);

            if (returnRequest == null || orderItem == null)
            {
                return NotFound();
            }

            var msg = await _messageFactory.SendReturnRequestStatusChangedCustomerNotificationAsync(returnRequest, orderItem, Services.WorkContext.WorkingLanguage.Id);
            if (msg?.Email?.Id != null)
            {
                NotifySuccess(T("Admin.ReturnRequests.Notified"));
            }

            return RedirectToAction(nameof(Edit), returnRequest.Id);
        }

        [HttpPost]
        [Permission(Permissions.Order.ReturnRequest.Accept)]
        public async Task<IActionResult> Accept(UpdateOrderItemModel model)
        {
            var returnRequest = await _db.ReturnRequests
                .IncludeCustomer()
                .FindByIdAsync(model.Id);

            var orderItem = await _db.OrderItems
                .Include(x => x.Order)
                .FindByIdAsync(returnRequest?.OrderItemId ?? 0);

            if (returnRequest == null || orderItem == null)
            {
                return NotFound();
            }

            var cancelQuantity = returnRequest.Quantity > orderItem.Quantity ? orderItem.Quantity : returnRequest.Quantity;

            var context = new UpdateOrderDetailsContext
            {
                OldQuantity = orderItem.Quantity,
                NewQuantity = Math.Max(orderItem.Quantity - cancelQuantity, 0),
                AdjustInventory = model.AdjustInventory,
                UpdateRewardPoints = model.UpdateRewardPoints,
                UpdateTotals = model.UpdateTotals
            };

            returnRequest.ReturnRequestStatus = ReturnRequestStatus.ReturnAuthorized;

            // INFO: UpdateOrderDetailsAsync performs commit.
            await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), orderItem.Order.GetOrderNumber());
            TempData[UpdateOrderDetailsContext.InfoKey] = context.ToString(Services.Localization);

            return RedirectToAction(nameof(Edit), new { id = returnRequest.Id });
        }

        [HttpPost]
        [Permission(Permissions.Order.ReturnRequest.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var returnRequest = await _db.ReturnRequests.FindByIdAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            _db.ReturnRequests.Remove(returnRequest);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteReturnRequest, T("ActivityLog.DeleteReturnRequest"), id);
            NotifySuccess(T("Admin.ReturnRequests.Deleted"));

            return RedirectToAction(nameof(List));
        }

        private async Task<ReturnRequestModel> PrepareReturnRequestModel(
            ReturnRequestModel model,
            ReturnRequest returnRequest,
            bool excludeProperties = false)
        {
            var allStores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            var orderItem = await _db.OrderItems
                .Include(x => x.Product)
                .Include(x => x.Order)
                .FindByIdAsync(returnRequest.OrderItemId);

            await PrepareReturnRequestModel(model, returnRequest, orderItem, allStores, excludeProperties);

            return model;
        }

        private async Task PrepareReturnRequestModel(
            ReturnRequestModel model,
            ReturnRequest returnRequest,
            OrderItem orderItem,
            Dictionary<int, Store> allStores,
            bool excludeProperties = false,
            bool forList = false)
        {
            Guard.NotNull(returnRequest, nameof(returnRequest));

            var store = allStores.Get(returnRequest.StoreId);
            var order = orderItem?.Order;
            var customer = returnRequest.Customer;

            model.Id = returnRequest.Id;
            model.ProductId = orderItem?.ProductId ?? 0;
            model.ProductSku = orderItem?.Product?.Sku;
            model.ProductName = orderItem?.Product?.Name;
            model.ProductTypeName = orderItem?.Product?.GetProductTypeLabel(Services.Localization);
            model.ProductTypeLabelHint = orderItem?.Product?.ProductTypeLabelHint;
            model.AttributeInfo = orderItem?.AttributeDescription;
            model.OrderId = orderItem?.OrderId ?? 0;
            model.OrderNumber = order?.GetOrderNumber();
            model.CustomerId = returnRequest.CustomerId;
            model.CustomerFullName = customer.GetFullName().NullEmpty() ?? customer.FindEmail().NaIfEmpty();
            model.CanSendEmailToCustomer = customer.FindEmail().HasValue();
            model.Quantity = returnRequest.Quantity;
            model.ReturnRequestStatusString = await Services.Localization.GetLocalizedEnumAsync(returnRequest.ReturnRequestStatus);
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(returnRequest.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(returnRequest.UpdatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action(nameof(Edit), "ReturnRequest", new { id = returnRequest.Id });
            model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = returnRequest.CustomerId });

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
                model.ReasonForReturn = returnRequest.ReasonForReturn;
                model.RequestedAction = returnRequest.RequestedAction;

                if (returnRequest.RequestedActionUpdatedOnUtc.HasValue)
                {
                    model.RequestedActionUpdated = Services.DateTimeHelper.ConvertToUserTime(returnRequest.RequestedActionUpdatedOnUtc.Value, DateTimeKind.Utc);
                }

                model.CustomerComments = returnRequest.CustomerComments;
                model.StaffNotes = returnRequest.StaffNotes;
                model.AdminComment = returnRequest.AdminComment;
                model.ReturnRequestStatusId = returnRequest.ReturnRequestStatusId;
            }

            if (!forList)
            {
                string returnRequestReasons = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestReasons, order?.CustomerLanguageId, store?.Id, true, false);
                string returnRequestActions = _orderSettings.GetLocalizedSetting(x => x.ReturnRequestActions, order?.CustomerLanguageId, store?.Id, true, false);
                string unspec = T("Common.Unspecified");

                var reasonForReturn = returnRequestReasons.SplitSafe(',')
                    .Select(x => new SelectListItem { Text = x, Value = x, Selected = x == returnRequest.ReasonForReturn })
                    .ToList();
                reasonForReturn.Insert(0, new SelectListItem { Text = unspec, Value = string.Empty });

                var actionsForReturn = returnRequestActions.SplitSafe(',')
                    .Select(x => new SelectListItem { Text = x, Value = x, Selected = x == returnRequest.RequestedAction })
                    .ToList();
                actionsForReturn.Insert(0, new SelectListItem { Text = unspec, Value = string.Empty });

                ViewBag.ReasonForReturn = reasonForReturn;
                ViewBag.ActionsForReturn = actionsForReturn;

                model.UpdateOrderItem = new UpdateOrderItemModel
                {
                    Id = returnRequest.Id,
                    Caption = T("Admin.ReturnRequests.Accept.Caption"),
                    PostUrl = Url.Action("Accept", "ReturnRequest"),
                };

                if (order != null)
                {
                    model.UpdateOrderItem.UpdateRewardPoints = order.RewardPointsWereAdded;
                    model.UpdateOrderItem.UpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
                    model.UpdateOrderItem.ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
                    model.UpdateOrderItem.ShowUpdateRewardPoints = order.OrderStatusId > (int)OrderStatus.Pending && order.RewardPointsWereAdded;
                }

                model.ReturnRequestInfo = TempData[UpdateOrderDetailsContext.InfoKey] as string;

                // The maximum amount that can be refunded for this return request.
                if (orderItem != null)
                {
                    var maxRefundAmount = Math.Max(orderItem.UnitPriceInclTax * returnRequest.Quantity, 0);
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
