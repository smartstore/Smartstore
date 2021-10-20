using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class OrderController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly ModuleManager _moduleManager;

        public OrderController(
            SmartDbContext db,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ModuleManager moduleManager)
        {
            _db = db;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
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

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-order-by-number")]
        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> GoToOrderId(OrderListModel model)
        {
            var orderId = 0;

            if (model.GoDirectlyToNumber.HasValue())
            {
                orderId = await _db.Orders
                    .Where(x => x.OrderNumber == model.GoDirectlyToNumber)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                if (orderId == 0 && int.TryParse(model.GoDirectlyToNumber, out orderId) && orderId > 0)
                {
                    if (!await _db.Orders.AnyAsync(x => x.Id == orderId))
                    {
                        orderId = 0;
                    }
                }
            }

            if (orderId != 0)
            {
                return RedirectToAction("Edit", new { id = orderId });
            }

            NotifyWarning(T("Admin.Order.NotFound"));

            return RedirectToAction("List");
        }

        [HttpPost]
        [Permission(Permissions.Order.Read)]
        public IActionResult ExportPdf(string selectedIds)
        {
            return RedirectToAction("PrintMany", "Order", new { ids = selectedIds, pdf = true, area = string.Empty });
        }

        #region Payment

        [HttpPost]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> ProcessOrder(string operation, string selectedIds)
        {
            var ids = selectedIds.ToIntArray();
            var orders = await _db.Orders.GetManyAsync(ids, true);
            if (!orders.Any() || operation.IsEmpty())
            {
                return RedirectToAction("List");
            }

            const int maxErrors = 3;
            var success = 0;
            var skipped = 0;
            var errors = 0;
            var errorMessages = new HashSet<string>();
            var succeededOrderNumbers = new HashSet<string>();

            foreach (var o in orders)
            {
                try
                {
                    var succeeded = false;

                    switch (operation)
                    {
                        case "cancel":
                            if (o.CanCancelOrder())
                            {
                                await _orderProcessingService.CancelOrderAsync(o, true);
                                succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "complete":
                            if (o.CanCompleteOrder())
                            {
                                await _orderProcessingService.CompleteOrderAsync(o);
                                succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "markpaid":
                            if (o.CanMarkOrderAsPaid())
                            {
                                await _orderProcessingService.MarkOrderAsPaidAsync(o);
                                succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "capture":
                            if (await _orderProcessingService.CanCaptureAsync(o))
                            {
                                var captureErrors = await _orderProcessingService.CaptureAsync(o);
                                errorMessages.AddRange(captureErrors);
                                if (!captureErrors.Any())
                                    succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "refundoffline":
                            if (o.CanRefundOffline())
                            {
                                await _orderProcessingService.RefundOfflineAsync(o);
                                succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "refund":
                            if (await _orderProcessingService.CanRefundAsync(o))
                            {
                                var refundErrors = await _orderProcessingService.RefundAsync(o);
                                errorMessages.AddRange(refundErrors);
                                if (!refundErrors.Any())
                                    succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "voidoffline":
                            if (o.CanVoidOffline())
                            {
                                await _orderProcessingService.VoidOfflineAsync(o);
                                succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                        case "void":
                            if (await _orderProcessingService.CanVoidAsync(o))
                            {
                                var voidErrors = await _orderProcessingService.VoidAsync(o);
                                errorMessages.AddRange(voidErrors);
                                if (!voidErrors.Any())
                                    succeeded = true;
                            }
                            else
                            {
                                ++skipped;
                            }
                            break;
                    }

                    if (succeeded)
                    {
                        ++success;
                        succeededOrderNumbers.Add(o.GetOrderNumber());
                    }
                }
                catch (Exception ex)
                {
                    errorMessages.Add(ex.Message);
                    if (++errors <= maxErrors)
                    {
                        Logger.Error(ex);
                    }
                }
            }

            using var psb = StringBuilderPool.Instance.Get(out var msg);
            msg.Append(T("Admin.Orders.ProcessingResult", success, ids.Length, skipped, skipped == 0 ? " class='hide'" : ""));
            errorMessages.Take(maxErrors).Each(x => msg.Append($"<div class='text-danger mt-2'>{x}</div>"));

            NotifyInfo(msg.ToString());

            if (succeededOrderNumbers.Any())
            {
                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), string.Join(", ", succeededOrderNumbers.OrderBy(x => x)));
            }

            return RedirectToAction("List");
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("cancelorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.CancelOrderAsync(order, true);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("completeorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.CompleteOrderAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("captureorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> CaptureOrder(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                var errors = await _orderProcessingService.CaptureAsync(order);
                foreach (var error in errors)
                {
                    NotifyError(error);
                }

                if (!errors.Any())
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("markorderaspaid")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> MarkOrderAsPaid(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.MarkOrderAsPaidAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("refundorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> RefundOrder(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                var errors = await _orderProcessingService.RefundAsync(order);
                foreach (var error in errors)
                {
                    NotifyError(error);
                }

                if (!errors.Any())
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("refundorderoffline")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> RefundOrderOffline(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.RefundOfflineAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("voidorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> VoidOrder(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                var errors = await _orderProcessingService.VoidAsync(order);
                foreach (var error in errors)
                {
                    NotifyError(error);
                }

                if (!errors.Any())
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("voidorderoffline")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> VoidOrderOffline(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.VoidOfflineAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction("Edit", new { id });
        }

        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> PartiallyRefundOrderPopup(int id, bool online)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderModel();
            PrepareOrderDetailsModel(model, order);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("partialrefundorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> PartiallyRefundOrderPopup(string btnId, string formId, int id, bool online, OrderModel model)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            try
            {
                IList<string> errors = null;
                var amountToRefund = model.AmountToRefund;
                var maxAmountToRefund = order.OrderTotal - order.RefundedAmount;

                if (amountToRefund > maxAmountToRefund)
                {
                    amountToRefund = maxAmountToRefund;
                }

                if (amountToRefund <= decimal.Zero)
                {
                    errors = new List<string> { T("Admin.OrderNotice.RefundAmountError") };
                }
                else if (online)
                {
                    errors = await _orderProcessingService.PartiallyRefundAsync(order, amountToRefund);
                }
                else
                {
                    await _orderProcessingService.PartiallyRefundOfflineAsync(order, amountToRefund);
                }

                if (errors?.Any() ?? false)
                {
                    foreach (var error in errors)
                    {
                        NotifyError(error, false);
                    }
                }
                else
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

                    ViewBag.RefreshPage = true;
                    ViewBag.btnId = btnId;
                    ViewBag.formId = formId;
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex, false);
            }

            PrepareOrderDetailsModel(model, order);

            return View(model);
        }

        #endregion

        #region Utilities

        private void PrepareOrderDetailsModel(OrderModel model, Order order)
        {
            //...
        }

        #endregion
    }
}
