using System;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
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
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer).ThenInclude(x => x.BillingAddress)
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer).ThenInclude(x => x.ShippingAddress)
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

            var rows = await recurringPayments.SelectAsync(async x =>
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
        public Task<IActionResult> Edit(int id)
        {
            Task.Delay(10);
            throw new NotImplementedException();
        }


        private async Task PrepareRecurringPaymentModel(RecurringPaymentModel model, RecurringPayment recurringPayment, bool forList)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            var initialOrder = recurringPayment.InitialOrder;
            var customer = initialOrder?.Customer;

            model.Id = recurringPayment.Id;
            model.CycleLength = recurringPayment.CycleLength;
            model.CyclePeriodId = recurringPayment.CyclePeriodId;
            model.CyclePeriodString = await Services.Localization.GetLocalizedEnumAsync(recurringPayment.CyclePeriod);
            model.TotalCycles = recurringPayment.TotalCycles;
            model.StartDate = Services.DateTimeHelper.ConvertToUserTime(recurringPayment.StartDateUtc, DateTimeKind.Utc);
            model.IsActive = recurringPayment.IsActive;
            model.NextPaymentDate = Services.DateTimeHelper.ConvertToUserTime(recurringPayment.NextPaymentDate.Value, DateTimeKind.Utc);
            model.CyclesRemaining = recurringPayment.CyclesRemaining;
            model.InitialOrderId = recurringPayment.InitialOrderId;
            model.InitialOrderNumber = initialOrder?.GetOrderNumber();
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(recurringPayment.CreatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action(nameof(Edit), "ReturnRequest", new { id = recurringPayment.Id });

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
                    .SelectAsync(async x =>
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
            }
        }
    }
}
