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

            var rows = await recurringPayments.SelectAwait(async x =>
            {
                var m = new RecurringPaymentModel();
                await PrepareRecurringPaymentModel(m, x, true);
                return m;
            })
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

            var model = new RecurringPaymentModel();
            await PrepareRecurringPaymentModel(model, recurringPayment, false);

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

            var rows = await recurringPayment.RecurringPaymentHistory.SelectAwait(async x =>
            {
                var m = new RecurringPaymentModel.RecurringPaymentHistoryModel();
                await PrepareRecurringPaymentHistoryModel(m, x, orders.Get(x.OrderId));
                return m;
            })
            .AsyncToList();

            return Json(new GridModel<RecurringPaymentModel.RecurringPaymentHistoryModel>
            {
                Rows = rows,
                Total = recurringPayment.RecurringPaymentHistory.Count
            });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("processnextpayment")]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> ProcessNextPayment(int id)
        {
            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer)
                .FindByIdAsync(id);

            if (recurringPayment == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.ProcessNextRecurringPaymentAsync(recurringPayment);

                NotifySuccess(T("Admin.RecurringPayments.NextPaymentProcessed"));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), recurringPayment.Id);
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("cancelpayment")]
        [Permission(Permissions.Order.EditRecurringPayment)]
        public async Task<IActionResult> CancelRecurringPayment(int id)
        {
            var recurringPayment = await _db.RecurringPayments
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer)
                .FindByIdAsync(id);

            if (recurringPayment == null)
            {
                return NotFound();
            }

            try
            {
                var errors = await _orderProcessingService.CancelRecurringPaymentAsync(recurringPayment);

                if (errors.Any())
                {
                    errors.Each(x => NotifyError(x));
                }
                else
                {
                    NotifySuccess(T("Admin.RecurringPayments.Cancelled"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), recurringPayment.Id);
        }

        private async Task PrepareRecurringPaymentModel(RecurringPaymentModel model, RecurringPayment recurringPayment, bool forList)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            var initialOrder = recurringPayment.InitialOrder;
            var customer = initialOrder?.Customer;
            var nextPaymentDate = await _paymentService.GetNextRecurringPaymentDateAsync(recurringPayment);

            model.Id = recurringPayment.Id;
            model.CycleLength = recurringPayment.CycleLength;
            model.CyclePeriodId = recurringPayment.CyclePeriodId;
            model.CyclePeriodString = await Services.Localization.GetLocalizedEnumAsync(recurringPayment.CyclePeriod);
            model.TotalCycles = recurringPayment.TotalCycles;
            model.StartDate = Services.DateTimeHelper.ConvertToUserTime(recurringPayment.StartDateUtc, DateTimeKind.Utc);
            model.IsActive = recurringPayment.IsActive;
            model.CyclesRemaining = await _paymentService.GetRecurringPaymentRemainingCyclesAsync(recurringPayment);
            model.InitialOrderId = recurringPayment.InitialOrderId;
            model.InitialOrderNumber = initialOrder?.GetOrderNumber();
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(recurringPayment.CreatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action(nameof(Edit), "RecurringPayment", new { id = recurringPayment.Id });

            model.NextPaymentDate = nextPaymentDate.HasValue
                ? Services.DateTimeHelper.ConvertToUserTime(nextPaymentDate.Value, DateTimeKind.Utc)
                : null;

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
                var currentCustomer = Services.WorkContext.CurrentCustomer;

                model.CanCancel = await _orderProcessingService.CanCancelRecurringPaymentAsync(recurringPayment, currentCustomer);

                if (initialOrder != null)
                {
                    var paymentType = await _paymentService.GetRecurringPaymentTypeAsync(initialOrder.PaymentMethodSystemName);
                    model.PaymentType = await Services.Localization.GetLocalizedEnumAsync(paymentType);
                }

                var orderIds = recurringPayment.RecurringPaymentHistory.ToDistinctArray(x => x.OrderId);
                var orders = await _db.Orders
                    .AsNoTracking()
                    .Where(x => orderIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x);

                model.History = await recurringPayment.RecurringPaymentHistory
                    .OrderBy(x => x.CreatedOnUtc)
                    .SelectAwait(async x =>
                    {
                        var m = new RecurringPaymentModel.RecurringPaymentHistoryModel();
                        await PrepareRecurringPaymentHistoryModel(m, x, orders.Get(x.OrderId));
                        return m;
                    })
                    .AsyncToList();
            }
        }

        private async Task PrepareRecurringPaymentHistoryModel(
            RecurringPaymentModel.RecurringPaymentHistoryModel model,
            RecurringPaymentHistory history,
            Order order)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(history, nameof(history));

            model.Id = history.Id;
            model.OrderId = history.OrderId;
            model.RecurringPaymentId = history.RecurringPaymentId;
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(history.CreatedOnUtc, DateTimeKind.Utc);

            if (order != null)
            {
                model.OrderStatus = await Services.Localization.GetLocalizedEnumAsync(order.OrderStatus);
                model.PaymentStatus = await Services.Localization.GetLocalizedEnumAsync(order.PaymentStatus);
                model.ShippingStatus = await Services.Localization.GetLocalizedEnumAsync(order.ShippingStatus);
                model.OrderNumber = order.GetOrderNumber();
                model.OrderEditUrl = Url.Action("Edit", "Order", new { id = order.Id });
            }
        }
    }
}
