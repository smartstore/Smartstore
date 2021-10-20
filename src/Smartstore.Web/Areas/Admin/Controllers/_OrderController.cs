using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class OrderController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly ModuleManager _moduleManager;

        public OrderController(
            SmartDbContext db,
            IOrderService orderService,
            IPaymentService paymentService,
            ModuleManager moduleManager)
        {
            _db = db;
            _orderService = orderService;
            _paymentService = paymentService;
            _moduleManager = moduleManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> List()
        {
            var allPaymentMethods = await _paymentService.LoadAllPaymentMethodsAsync();

            var paymentMethods = allPaymentMethods
                .Select(x => new SelectListItem
                {
                    Text = (_moduleManager.GetLocalizedFriendlyName(x.Metadata).NullEmpty() ?? x.Metadata.FriendlyName.NullEmpty() ?? x.Metadata.SystemName).EmptyNull(),
                    Value = x.Metadata.SystemName
                })
                .ToList();

            var paymentMethodsCounts = paymentMethods
                .GroupBy(x => x.Text)
                .Select(x => new { Name = x.Key.EmptyNull(), Count = x.Count() })
                .ToDictionarySafe(x => x.Name, x => x.Count);

            // Append system name if there are payment methods with the same friendly name.
            paymentMethods = paymentMethods
                .OrderBy(x => x.Text)
                .Select(x =>
                {
                    if (paymentMethodsCounts.TryGetValue(x.Text, out var count) && count > 1)
                    {
                        x.Text = $"{x.Text} ({x.Value})";
                    }

                    return x;
                })
                .ToList();

            ViewBag.PaymentMethods = paymentMethods;
            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();
            ViewBag.HideProfitReport = false;

            return View(new OrderListModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> OrderList(GridCommand command, OrderListModel model)
        {
            var dtHelper = Services.DateTimeHelper;
            var primaryCurrency = Services.CurrencyService.PrimaryCurrency;
            var stores = Services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            var viaShippingMethodString = T("Admin.Order.ViaShippingMethod").Value;
            var withPaymentMethodString = T("Admin.Order.WithPaymentMethod").Value;
            var fromStoreString = T("Admin.Order.FromStore").Value;
            var paymentMethodSystemnames = model.PaymentMethods.SplitSafe(',').ToArray();

            DateTime? startDateUtc = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateUtc = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            // Create order query.
            var orderQuery = _db.Orders
                .Include(x => x.OrderItems)
                .IncludeBillingAddress()
                .IncludeShippingAddress()
                .AsNoTracking()
                .ApplyAuditDateFilter(startDateUtc, endDateUtc)
                .ApplyStatusFilter(model.OrderStatusIds, model.PaymentStatusIds, model.ShippingStatusIds)
                .ApplyPaymentFilter(paymentMethodSystemnames);

            if (model.CustomerEmail.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilterFor(x => x.BillingAddress.Email, model.CustomerEmail);
            }
            if (model.CustomerName.HasValue())
            {
                // InvalidOperationException: The binary operator OrElse is not defined for...
                //orderQuery = orderQuery.ApplySearchFilter(
                //    model.CustomerName,
                //    LogicalRuleOperator.Or, 
                //    x => x.BillingAddress.FirstName, 
                //    x => x.BillingAddress.LastName);

                orderQuery = orderQuery.Where(x => x.BillingAddress.LastName.Contains(model.CustomerName) || x.BillingAddress.FirstName.Contains(model.CustomerName));
            }
            if (model.OrderGuid.HasValue())
            {
                orderQuery = orderQuery.Where(x => x.OrderGuid.ToString().Contains(model.OrderGuid));
            }
            if (model.OrderNumber.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilterFor(x => x.OrderNumber, model.OrderNumber);
            }
            if (model.StoreId > 0)
            {
                orderQuery = orderQuery.Where(x => x.StoreId == model.StoreId);
            }

            orderQuery = orderQuery
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false);

            var orders = await orderQuery
                .ToPagedList(command)
                .LoadAsync();

            var paymentMethods = await orders
                .Where(x => x.PaymentMethodSystemName.HasValue())
                .Select(x => x.PaymentMethodSystemName)
                .Distinct()
                .SelectAsync(async x => await _paymentService.LoadPaymentMethodBySystemNameAsync(x))
                .AsyncToList();

            var paymentMethodsDic = paymentMethods
                .Where(x => x != null)
                .ToDictionarySafe(
                    x => x.Metadata.SystemName,
                    x => _moduleManager.GetLocalizedFriendlyName(x.Metadata), 
                    StringComparer.OrdinalIgnoreCase);

            var rows = await orders.SelectAsync(async x =>
            {
                stores.TryGetValue(x.StoreId, out var store);
                paymentMethodsDic.TryGetValue(x.PaymentMethodSystemName, out var paymentMethod);

                var shipTo = x.ShippingAddress;
                var createdOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);
                var updatedOn = Services.DateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc);
                var orderTotal = primaryCurrency.AsMoney(x.OrderTotal);

                var m = new OrderOverviewModel
                {
                    Id = x.Id,
                    OrderNumber = x.GetOrderNumber(),
                    OrderGuid = x.OrderGuid,
                    StoreName = store?.Name?.NaIfEmpty(),
                    CustomerName = x.BillingAddress.GetFullName(),
                    CustomerEmail = x.BillingAddress.Email,
                    VatNumber = x.VatNumber,
                    OrderTotal = orderTotal,
                    OrderTotalString = orderTotal.ToString(true),
                    OrderStatus = x.OrderStatus,
                    OrderStatusString = await Services.Localization.GetLocalizedEnumAsync(x.OrderStatus),
                    PaymentStatus = x.PaymentStatus,
                    PaymentStatusString = await Services.Localization.GetLocalizedEnumAsync(x.PaymentStatus),
                    PaymentMethod = paymentMethod.NullEmpty() ?? x.PaymentMethodSystemName,
                    PaymentMethodSystemName = x.PaymentMethodSystemName,
                    HasNewPaymentNotification = x.HasNewPaymentNotification,
                    ShippingStatus = x.ShippingStatus,
                    ShippingStatusString = await Services.Localization.GetLocalizedEnumAsync(x.ShippingStatus),
                    IsShippable = x.ShippingStatus != ShippingStatus.ShippingNotRequired,
                    ShippingMethod = x.ShippingMethod.NaIfEmpty(),
                    CreatedOn = createdOn,
                    CreatedOnString = createdOn.ToString("g"),
                    UpdatedOn = updatedOn,
                    UpdatedOnString = updatedOn.ToString("g"),
                    EditUrl = Url.Action("Edit", "Order", new { id = x.Id })
                };

                m.ViaShippingMethod = viaShippingMethodString.FormatInvariant(m.ShippingMethod);
                m.WithPaymentMethod = withPaymentMethodString.FormatInvariant(m.PaymentMethod);
                m.FromStore = fromStoreString.FormatInvariant(m.StoreName);

                if (shipTo != null && m.IsShippable)
                {
                    m.ShippingAddressString = $"{shipTo.Address1}, {shipTo.ZipPostalCode} {shipTo.City}";
                    if (shipTo.Country != null)
                    {
                        m.ShippingAddressString += ", " + shipTo.Country.TwoLetterIsoCode;
                    }
                }

                return m;
            })
            .AsyncToList();

            var productCost = await orderQuery.GetOrdersProductCostsAsync();
            var summary = await orderQuery.SelectAsOrderAverageReportLine().FirstOrDefaultAsync() ?? new OrderAverageReportLine();
            var profit = summary.SumOrderTotal - summary.SumTax - productCost;

            ViewBag.SumOrderTax = primaryCurrency.AsMoney(summary.SumTax).ToString(true);
            ViewBag.SumOrderTotal = primaryCurrency.AsMoney(summary.SumOrderTotal).ToString(true);
            ViewBag.SumProfit = primaryCurrency.AsMoney(profit).ToString(true);

            return Json(new GridModel<OrderOverviewModel>
            {
                Rows = rows,
                Total = orders.TotalCount
            });
        }
    }
}
