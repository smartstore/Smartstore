using System.Data;
using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class RecurringPaymentController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;

        public RecurringPaymentController(
            SmartDbContext db,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService)
        {
            _db = db;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Order.Read)]
        public IActionResult List()
        {
            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();

            return View(new RecurringPaymentListModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> RecurringPaymentList(GridCommand command, RecurringPaymentListModel model)
        {
            var query = _db.RecurringPayments
                .IncludeAddresses()
                .AsQueryable();

            if (model.CustomerEmail.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.InitialOrder.Customer.BillingAddress.Email, model.CustomerEmail);
            }

            if (model.CustomerName.HasValue())
            {
                query = query.Where(x =>
                    x.InitialOrder.Customer.BillingAddress.LastName.Contains(model.CustomerName) ||
                    x.InitialOrder.Customer.BillingAddress.FirstName.Contains(model.CustomerName));
            }

            if (model.RemainingCycles != null)
            {
                query = model.RemainingCycles == true
                    ? query.Where(x => x.IsActive && x.RecurringPaymentHistory.Count < x.TotalCycles)
                    : query.Where(x => !x.IsActive || x.RecurringPaymentHistory.Count >= x.TotalCycles);
            }

            if (model.InitialOrderNumber.HasValue())
            {
                query = int.TryParse(model.InitialOrderNumber, out var orderId) && orderId != 0
                    ? query = query.Where(x => x.InitialOrder.OrderNumber.Contains(model.InitialOrderNumber) || x.InitialOrderId == orderId)
                    : query = query.Where(x => x.InitialOrder.OrderNumber.Contains(model.InitialOrderNumber));
            }

            var recurringPayments = await query
                .ApplyStandardFilter(null, null, model.StoreId, true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await recurringPayments
                .SelectAwait(async x => await CreateRecurringPaymentModel(x, true))
                .AsyncToList();

            return Json(new GridModel<RecurringPaymentModel>
            {
                Rows = rows,
                Total = recurringPayments.TotalCount
            });
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var recurringPayment = await _db.RecurringPayments
                .AsSplitQuery()
                .IncludeAddresses()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (recurringPayment == null)
            {
                return NotFound();
            }

            var model = await CreateRecurringPaymentModel(recurringPayment, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> Edit(RecurringPaymentModel model, bool continueEditing)
        {
            var recurringPayment = await _db.RecurringPayments.FindByIdAsync(model.Id);
            if (recurringPayment == null || recurringPayment.Deleted)
            {
                return NotFound();
            }

            recurringPayment.CycleLength = model.CycleLength;
            recurringPayment.CyclePeriodId = model.CyclePeriodId;
            recurringPayment.TotalCycles = model.TotalCycles;
            recurringPayment.IsActive = model.IsActive;

            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.RecurringPayments.Updated"));

            return continueEditing
                ? RedirectToAction(nameof(Edit), recurringPayment.Id)
                : RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> Delete(int id)
        {
            var recurringPayment = await _db.RecurringPayments.FindByIdAsync(id);
            if (recurringPayment == null)
            {
                return NotFound();
            }

            recurringPayment.Deleted = true;
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.RecurringPayments.Deleted"));

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> RecurringPaymentHistoryList(int recurringPaymentId)
        {
            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.RecurringPaymentHistory)
                .FindByIdAsync(recurringPaymentId);
            if (recurringPayment == null)
            {
                return NotFound();
            }

            var orderIds = recurringPayment.RecurringPaymentHistory.ToDistinctArray(x => x.OrderId);
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x);

            var rows = recurringPayment.RecurringPaymentHistory
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => CreateRecurringPaymentHistoryModel(x, orders.Get(x.OrderId)))
                .ToList();

            return Json(new GridModel<RecurringPaymentHistoryModel>
            {
                Rows = rows,
                Total = recurringPayment.RecurringPaymentHistory.Count
            });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> ProcessNextPayment(int id)
        {
            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.RecurringPaymentHistory)
                .Include(x => x.InitialOrder)
                .ThenInclude(x => x.Customer)
                .FindByIdAsync(id);
            if (recurringPayment == null)
            {
                return NotFound();
            }

            string message;
            var success = false;

            try
            {
                await _orderProcessingService.ProcessNextRecurringPaymentAsync(recurringPayment);

                message = T("Admin.RecurringPayments.NextPaymentProcessed");
                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                message = ex.Message;
            }

            return Json(new { success, message });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> CancelRecurringPayment(int id)
        {
            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.InitialOrder)
                .ThenInclude(x => x.Customer)
                .FindByIdAsync(id);
            if (recurringPayment == null)
            {
                return NotFound();
            }

            string message;
            var success = false;

            try
            {
                await _orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);

                message = T("Admin.RecurringPayments.Cancelled");
                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                message = ex.Message;
            }

            return Json(new { success, message });
        }

        private async Task<RecurringPaymentModel> CreateRecurringPaymentModel(RecurringPayment recurringPayment, bool forList)
        {
            Guard.NotNull(recurringPayment);

            var dtHelper = Services.DateTimeHelper;
            var initialOrder = recurringPayment.InitialOrder;
            var customer = initialOrder?.Customer;
            var nextPaymentDate = await _paymentService.GetNextRecurringPaymentDateAsync(recurringPayment);
            var canCancel = nextPaymentDate != null && await _orderProcessingService.CanCancelRecurringPaymentAsync(recurringPayment, Services.WorkContext.CurrentCustomer);

            var model = new RecurringPaymentModel
            {
                Id = recurringPayment.Id,
                CycleLength = recurringPayment.CycleLength,
                CyclePeriodId = recurringPayment.CyclePeriodId,
                CyclePeriodString = Services.Localization.GetLocalizedEnum(recurringPayment.CyclePeriod),
                TotalCycles = recurringPayment.TotalCycles,
                CyclesRemaining = await _paymentService.GetRecurringPaymentRemainingCyclesAsync(recurringPayment),
                StartDate = dtHelper.ConvertToUserTime(recurringPayment.StartDateUtc, DateTimeKind.Utc),
                IsActive = recurringPayment.IsActive,
                InitialOrderId = recurringPayment.InitialOrderId,
                InitialOrderNumber = initialOrder?.GetOrderNumber(),
                CreatedOn = dtHelper.ConvertToUserTime(recurringPayment.CreatedOnUtc, DateTimeKind.Utc),
                EditUrl = Url.Action(nameof(Edit), "RecurringPayment", new { id = recurringPayment.Id }),
                NextPaymentDateUtc = nextPaymentDate,
                NextPaymentDate = nextPaymentDate.HasValue ? dtHelper.ConvertToUserTime(nextPaymentDate.Value, DateTimeKind.Utc) : null,
                CanCancel = canCancel
            };

            if (initialOrder != null)
            {
                model.InitialOrderEditUrl = Url.Action("Edit", "Order", new { id = recurringPayment.InitialOrderId });
            }

            if (customer != null)
            {
                model.CustomerId = customer.Id;
                model.CustomerFullName = customer.GetFullName().NullEmpty() ?? customer.FindEmail().NaIfEmpty();
                model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = customer.Id });
            }

            if (!forList)
            {
                if (initialOrder != null)
                {
                    var paymentType = await _paymentService.GetRecurringPaymentTypeAsync(initialOrder.PaymentMethodSystemName);
                    model.PaymentType = Services.Localization.GetLocalizedEnum(paymentType);
                }

                var orderIds = recurringPayment.RecurringPaymentHistory.ToDistinctArray(x => x.OrderId);
                var orders = await _db.Orders
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x);

                model.History = recurringPayment.RecurringPaymentHistory
                    .OrderByDescending(x => x.CreatedOnUtc)
                    .Select(x => CreateRecurringPaymentHistoryModel(x, orders.Get(x.OrderId)))
                    .ToList();
            }

            return model;
        }

        private RecurringPaymentHistoryModel CreateRecurringPaymentHistoryModel(RecurringPaymentHistory history, Order order)
        {
            Guard.NotNull(history);

            var model = new RecurringPaymentHistoryModel
            {
                Id = history.Id,
                OrderId = history.OrderId,
                RecurringPaymentId = history.RecurringPaymentId,
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(history.CreatedOnUtc, DateTimeKind.Utc)
            };

            if (order != null)
            {
                model.OrderStatus = order.OrderStatus;
                model.OrderStatusString = Services.Localization.GetLocalizedEnum(order.OrderStatus);
                model.PaymentStatus = order.PaymentStatus;
                model.PaymentStatusString = Services.Localization.GetLocalizedEnum(order.PaymentStatus);
                model.ShippingStatus = order.ShippingStatus;
                model.ShippingStatusString = Services.Localization.GetLocalizedEnum(order.ShippingStatus);
                model.OrderNumber = order.GetOrderNumber();
                model.OrderEditUrl = Url.Action("Edit", "Order", new { id = order.Id });
            }

            return model;
        }
    }
}
