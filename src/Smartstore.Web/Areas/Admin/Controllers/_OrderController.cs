using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Models;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Stores;
using Smartstore.Web.Rendering;
using Smartstore.Core.Checkout.Payment;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Engine.Modularity;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Collections;
using Smartstore.Core.Rules.Filters;

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
            var stores = Services.StoreContext.GetAllStores().ToDictionarySafe(x => x.Id, x => x);
            var viaShippingMethodString = T("Admin.Order.ViaShippingMethod").Value;
            var withPaymentMethodString = T("Admin.Order.WithPaymentMethod").Value;
            var fromStoreString = T("Admin.Order.FromStore").Value;
            var orderStatusIds = model.OrderStatusIds.ToIntArray();
            var paymentStatusIds = model.PaymentStatusIds.ToIntArray();
            var shippingStatusIds = model.ShippingStatusIds.ToIntArray();
            var paymentMethodSystemnames = model.PaymentMethods.SplitSafe(',').ToArray();
            var paymentMethods = new Dictionary<string, Provider<IPaymentMethod>>(StringComparer.OrdinalIgnoreCase);

            DateTime? startDateUtc = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDateUtc = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            // Create order query.
            var orderQuery = _db.Orders.AsNoTracking();

            // Product Id is only used and filled in context of product details (orders).
            // When no product Id is assigned, OrderList is used to prepare for orders list.
            var product = await _db.Products.FindByIdAsync(model.ProductId ?? 0, false);
            if (product == null)
            {
                orderQuery = orderQuery
                    .ApplyAuditDateFilter(startDateUtc, endDateUtc)
                    .ApplyStatusFilter(orderStatusIds, paymentStatusIds, shippingStatusIds)
                    .ApplyPaymentFilter(paymentMethodSystemnames);

                if (model.CustomerEmail.HasValue())
                {
                    // TODO: (mg) (core) test this. Do ApplySearchFilterFor work if the navigation property cannot be resolved (null check missing)?
                    orderQuery = orderQuery.ApplySearchFilterFor(x => x.BillingAddress.Email, model.CustomerEmail);
                }
                if (model.CustomerName.HasValue())
                {
                    // TODO: (mg) (core) test this too.
                    orderQuery = orderQuery.ApplySearchFilterFor(x => x.BillingAddress.LastName, model.CustomerName);
                    orderQuery = orderQuery.ApplySearchFilterFor(x => x.BillingAddress.FirstName, model.CustomerName);
                }
                if (model.OrderGuid.HasValue())
                {
                    // TODO: (mg) (core) test this too. Is it supported by EF Core?
                    // RE: I don't think so.
                    orderQuery = orderQuery.ApplySearchFilterFor(x => x.OrderGuid, model.OrderGuid);
                }
                if (model.OrderNumber.HasValue())
                {
                    orderQuery = orderQuery.ApplySearchFilterFor(x => x.OrderNumber, model.OrderNumber);
                }
                if (model.StoreId > 0)
                {
                    orderQuery = orderQuery.Where(x => x.StoreId == model.StoreId);
                }
            }
            else
            {
                orderQuery = orderQuery.Where(x => x.OrderItems.Any(p => p.ProductId == product.Id));
            }

            var orders = await orderQuery
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = orders.Select(x =>
            {
                stores.TryGetValue(x.StoreId, out var store);

                var m = new OrderOverviewModel
                {
                    Id = x.Id,
                    OrderNumber = x.GetOrderNumber(),
                    StoreName = store?.Name?.NaIfEmpty(),
                };

                //...

                return m;
            })
            .ToList();

            return Json(new GridModel<OrderOverviewModel>
            {
                Rows = rows,
                Total = orders.TotalCount
            });
        }
    }
}
