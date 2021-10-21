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
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class OrderController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IPaymentService _paymentService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IEncryptor _encryptor;
        private readonly ModuleManager _moduleManager;
        private readonly CatalogSettings _catalogSettings;
        private readonly TaxSettings _taxSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly PdfSettings _pdfSettings;
        private readonly AddressSettings _addressSettings;
        private readonly SearchSettings _searchSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly Currency _primaryCurrency;

        public OrderController(
            SmartDbContext db,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IEncryptor encryptor,
            ModuleManager moduleManager,
            CatalogSettings catalogSettings,
            TaxSettings taxSettings,
            MeasureSettings measureSettings,
            PdfSettings pdfSettings,
            AddressSettings addressSettings,
            SearchSettings searchSettings,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings)
        {
            _db = db;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _taxService = taxService;
            _encryptor = encryptor;
            _moduleManager = moduleManager;
            _catalogSettings = catalogSettings;
            _taxSettings = taxSettings;
            _measureSettings = measureSettings;
            _pdfSettings = pdfSettings;
            _addressSettings = addressSettings;
            _searchSettings = searchSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;                

            _primaryCurrency = currencyService.PrimaryCurrency;
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
                paymentMethodsDic.TryGetValue(x.PaymentMethodSystemName, out var paymentMethod);

                var shipTo = x.ShippingAddress;
                var m = new OrderOverviewModel();

                await PrepareOrderOverviewModel(m, x);

                m.PaymentMethod = paymentMethod.NullEmpty() ?? x.PaymentMethodSystemName;
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

            ViewBag.SumOrderTax = _primaryCurrency.AsMoney(summary.SumTax).ToString(true);
            ViewBag.SumOrderTotal = _primaryCurrency.AsMoney(summary.SumOrderTotal).ToString(true);
            ViewBag.SumProfit = _primaryCurrency.AsMoney(profit).ToString(true);

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
            var order = await GetOrderWithIncludes(id);
            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderModel();
            await PrepareOrderModel(model, order);

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("partialrefundorder")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> PartiallyRefundOrderPopup(string btnId, string formId, int id, bool online, OrderModel model)
        {
            var order = await GetOrderWithIncludes(id, true);
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

            await PrepareOrderModel(model, order);

            return View(model);
        }

        #endregion

        #region Edit, delete

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await GetOrderWithIncludes(id);
            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderModel();
            await PrepareOrderModel(model, order);

            return View(model);
        }


        #endregion

        #region Utilities

        private async Task<Order> GetOrderWithIncludes(int id, bool tracked = false)
        {
            var order = await _db.Orders
                .Include(x => x.RedeemedRewardPointsEntry)
                .IncludeOrderItems()
                .IncludeGiftCardHistory()
                .IncludeBillingAddress()
                .IncludeShippingAddress()
                .FindByIdAsync(id, tracked);

            return order;
        }

        private async Task PrepareOrderOverviewModel(OrderOverviewModel model, Order order)
        {
            MiniMapper.Map(order, model);

            model.OrderNumber = order.GetOrderNumber();
            model.StoreName = Services.StoreContext.GetStoreById(order.StoreId)?.Name ?? StringExtensions.NotAvailable;
            model.CustomerName = order.BillingAddress.GetFullName();
            model.CustomerEmail = order.BillingAddress.Email;
            model.OrderTotalString = Format(order.OrderTotal);
            model.OrderStatusString = await Services.Localization.GetLocalizedEnumAsync(order.OrderStatus);
            model.PaymentStatusString = await Services.Localization.GetLocalizedEnumAsync(order.PaymentStatus);
            model.ShippingStatusString = await Services.Localization.GetLocalizedEnumAsync(order.ShippingStatus);
            model.ShippingMethod = order.ShippingMethod.NaIfEmpty();
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(order.UpdatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action("Edit", "Order", new { id = order.Id });
        }

        private async Task PrepareOrderModel(OrderModel model, Order order)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(order, nameof(order));

            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.GetStoreById(order.StoreId);
            var taxRates = order.TaxRatesDictionary;

            MiniMapper.Map(order, model);
            await PrepareOrderOverviewModel(model, order);

            if (order.AffiliateId != 0)
            {
                var affiliate = await _db.Affiliates
                    .Include(x => x.Address)
                    .FindByIdAsync(order.AffiliateId);

                model.AffiliateFullName = affiliate?.Address?.GetFullName() ?? StringExtensions.NotAvailable;
            }

            model.DisplayPdfInvoice = _pdfSettings.Enabled;
            model.OrderSubtotalInclTaxString = Format(order.OrderSubtotalInclTax, true);
            model.OrderSubtotalExclTaxString = Format(order.OrderSubtotalExclTax, false);

            if (order.OrderSubTotalDiscountInclTax > decimal.Zero)
            {
                model.OrderSubTotalDiscountInclTaxString = Format(order.OrderSubTotalDiscountInclTax, true);
            }
            if (order.OrderSubTotalDiscountExclTax > decimal.Zero)
            {
                model.OrderSubTotalDiscountExclTaxString = Format(order.OrderSubTotalDiscountExclTax, false);
            }

            model.OrderShippingInclTaxString = Format(order.OrderShippingInclTax, true, PricingTarget.ShippingCharge);
            model.OrderShippingExclTaxString = Format(order.OrderShippingExclTax, false, PricingTarget.ShippingCharge);

            if (order.PaymentMethodAdditionalFeeInclTax != decimal.Zero)
            {
                model.PaymentMethodAdditionalFeeInclTaxString = Format(order.PaymentMethodAdditionalFeeInclTax, true, PricingTarget.PaymentFee);
                model.PaymentMethodAdditionalFeeExclTaxString = Format(order.PaymentMethodAdditionalFeeExclTax, false, PricingTarget.PaymentFee);
            }

            model.DisplayTaxRates = _taxSettings.DisplayTaxRates && taxRates.Any();
            model.DisplayTax = !model.DisplayTaxRates;
            model.TaxString = Format(order.OrderTax);

            model.TaxRates = taxRates
                .Select(x => new OrderModel.TaxRate
                {
                    Rate = _taxService.FormatTaxRate(x.Key),
                    Value = Format(x.Value)
                })
                .ToList();

            if (order.OrderDiscount > 0)
            {
                model.OrderDiscountString = Format(-order.OrderDiscount);
            }

            if (order.OrderTotalRounding != decimal.Zero)
            {
                model.OrderTotalRoundingString = Format(order.OrderTotalRounding);
            }

            model.GiftCards = order.GiftCardUsageHistory
                .Select(x => new OrderModel.GiftCard
                {
                    CouponCode = x.GiftCard.GiftCardCouponCode,
                    Amount = Format(-x.UsedValue)
                })
                .ToList();

            if (order.RedeemedRewardPointsEntry != null)
            {
                model.RedeemedRewardPoints = -order.RedeemedRewardPointsEntry.Points;
                model.RedeemedRewardPointsAmountString = Format(-order.RedeemedRewardPointsEntry.UsedAmount);
            }

            if (order.CreditBalance > decimal.Zero)
            {
                model.CreditBalanceString = Format(-order.CreditBalance);
            }

            if (order.RefundedAmount > decimal.Zero)
            {
                model.RefundedAmountString = Format(order.RefundedAmount);
            }

            if (order.AllowStoringCreditCardNumber)
            {
                model.AllowStoringCreditCardNumber = true;
                model.CardType = _encryptor.DecryptText(order.CardType);
                model.CardName = _encryptor.DecryptText(order.CardName);
                model.CardNumber = _encryptor.DecryptText(order.CardNumber);
                model.CardCvv2 = _encryptor.DecryptText(order.CardCvv2);

                var cardExpirationMonthDecrypted = _encryptor.DecryptText(order.CardExpirationMonth);
                if (cardExpirationMonthDecrypted.HasValue() && cardExpirationMonthDecrypted != "0")
                {
                    model.CardExpirationMonth = cardExpirationMonthDecrypted;
                }
                var cardExpirationYearDecrypted = _encryptor.DecryptText(order.CardExpirationYear);
                if (cardExpirationYearDecrypted.HasValue() && cardExpirationYearDecrypted != "0")
                {
                    model.CardExpirationYear = cardExpirationYearDecrypted;
                }
            }
            else
            {
                var maskedCreditCardNumberDecrypted = _encryptor.DecryptText(order.MaskedCreditCardNumber);
                if (maskedCreditCardNumberDecrypted.HasValue())
                {
                    model.CardNumber = maskedCreditCardNumberDecrypted;
                }
            }

            if (order.AllowStoringDirectDebit)
            {
                model.AllowStoringDirectDebit = true;
                model.DirectDebitAccountHolder = _encryptor.DecryptText(order.DirectDebitAccountHolder);
                model.DirectDebitAccountNumber = _encryptor.DecryptText(order.DirectDebitAccountNumber);
                model.DirectDebitBankCode = _encryptor.DecryptText(order.DirectDebitBankCode);
                model.DirectDebitBankName = _encryptor.DecryptText(order.DirectDebitBankName);
                model.DirectDebitBIC = _encryptor.DecryptText(order.DirectDebitBIC);
                model.DirectDebitCountry = _encryptor.DecryptText(order.DirectDebitCountry);
                model.DirectDebitIban = _encryptor.DecryptText(order.DirectDebitIban);
            }

            var pm = await _paymentService.LoadPaymentMethodBySystemNameAsync(order.PaymentMethodSystemName);
            if (pm != null)
            {
                model.DisplayCompletePaymentNote = order.PaymentStatus == PaymentStatus.Pending && await pm.Value.CanRePostProcessPaymentAsync(order);
                model.PaymentMethod = _moduleManager.GetLocalizedFriendlyName(pm.Metadata);
            }
            if (model.PaymentMethod.IsEmpty())
            {
                model.PaymentMethod = order.PaymentMethodSystemName;
            }

            // Purchase order number (we have to find a better to inject this information because it's related to a certain plugin).
            // TODO: (mg) (core) verify plugin systemname Smartstore.PurchaseOrderNumber.
            model.DisplayPurchaseOrderNumber = order.PaymentMethodSystemName.EqualsNoCase("Smartstore.PurchaseOrderNumber");

            model.CanCancelOrder = order.CanCancelOrder();
            model.CanCompleteOrder = order.CanCompleteOrder();
            model.CanCapture = await _orderProcessingService.CanCaptureAsync(order);
            model.CanMarkOrderAsPaid = order.CanMarkOrderAsPaid();
            model.CanRefund = await _orderProcessingService.CanRefundAsync(order);
            model.CanRefundOffline = order.CanRefundOffline();
            model.CanPartiallyRefund = await _orderProcessingService.CanPartiallyRefundAsync(order, decimal.Zero);
            model.CanPartiallyRefundOffline = order.CanPartiallyRefundOffline(decimal.Zero);
            model.CanVoid = await _orderProcessingService.CanVoidAsync(order);
            model.CanVoidOffline = order.CanVoidOffline();

            model.MaxAmountToRefund = order.OrderTotal - order.RefundedAmount;
            model.MaxAmountToRefundString = Format(model.MaxAmountToRefund);

            model.RecurringPaymentId = await _db.RecurringPayments
                .ApplyStandardFilter(order.Id, null, null, true)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            model.BillingAddress = MiniMapper.Map<Address, AddressModel>(order.BillingAddress);
            PrepareSettings(model.BillingAddress);

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                var shipTo = order.ShippingAddress;
                var googleAddressQuery = $"{shipTo.Address1} {shipTo.ZipPostalCode} {shipTo.City} {shipTo.Country?.Name ?? string.Empty}";

                model.ShippingAddress = MiniMapper.Map<Address, AddressModel>(shipTo);
                PrepareSettings(model.ShippingAddress);

                model.CanAddNewShipments = order.CanAddItemsToShipment();
                
                model.ShippingAddressGoogleMapsUrl = Services.ApplicationContext.AppConfiguration.Google.MapsUrl.FormatInvariant(
                    language.UniqueSeoCode.EmptyNull().ToLower(),
                    googleAddressQuery.UrlEncode());
            }

            model.CheckoutAttributeInfo = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(order.CheckoutAttributeDescription));

            foreach (var orderItem in order.OrderItems)
            {
                var product = orderItem.Product;

                if (product.IsDownload)
                {
                    model.HasDownloadableProducts = true;
                }

                await _productAttributeMaterializer.MergeWithCombinationAsync(product, orderItem.AttributeSelection);

                var orderItemModel = new OrderModel.OrderItemModel
                {
                    Id = orderItem.Id,
                    ProductId = orderItem.ProductId,
                    ProductName = product.GetLocalized(x => x.Name),
                    Sku = product.Sku,
                    ProductType = product.ProductType,
                    ProductTypeName = product.GetProductTypeLabel(Services.Localization),
                    ProductTypeLabelHint = product.ProductTypeLabelHint,
                    Quantity = orderItem.Quantity,
                    IsDownload = product.IsDownload,
                    DownloadCount = orderItem.DownloadCount,
                    DownloadActivationType = product.DownloadActivationType,
                    IsDownloadActivated = orderItem.IsDownloadActivated,
                    LicenseDownloadId = orderItem.LicenseDownloadId
                };

                if (product.ProductType == ProductType.BundledProduct && orderItem.BundleData.HasValue())
                {
                    var bundleData = orderItem.GetBundleData();

                    orderItemModel.BundlePerItemPricing = product.BundlePerItemPricing;
                    orderItemModel.BundlePerItemShoppingCart = bundleData.Any(x => x.PerItemShoppingCart);
                    orderItemModel.BundleItems = bundleData
                        .Select(x => new OrderModel.BundleItemModel
                        {
                            ProductId = x.ProductId,
                            Sku = x.Sku,
                            ProductName = x.ProductName,
                            ProductSeName = x.ProductSeName,
                            VisibleIndividually = x.VisibleIndividually,
                            Quantity = x.Quantity,
                            DisplayOrder = x.DisplayOrder,
                            AttributeInfo = x.AttributesInfo,
                            PriceWithDiscount = orderItemModel.BundlePerItemShoppingCart
                                ? Format(x.PriceWithDiscount)
                                : null
                        })
                        .ToList();
                }
                else
                {
                    orderItemModel.BundleItems = new();
                }

                //...
            }



            //...
        }

        private void PrepareSettings(AddressModel model)
        {
            model.ValidateEmailAddress = _addressSettings.ValidateEmailAddress;
            model.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.CompanyRequired = _addressSettings.CompanyRequired;
            model.CountryEnabled = _addressSettings.CountryEnabled;
            model.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.CityEnabled = _addressSettings.CityEnabled;
            model.CityRequired = _addressSettings.CityRequired;
            model.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.PhoneRequired = _addressSettings.PhoneRequired;
            model.FaxEnabled = _addressSettings.FaxEnabled;
            model.FaxRequired = _addressSettings.FaxRequired;
        }

        private string Format(decimal value, bool priceIncludesTax, PricingTarget target = PricingTarget.Product)
        {
            var format = _currencyService.GetTaxFormat(null, priceIncludesTax, target, Services.WorkContext.WorkingLanguage);

            return new Money(value, _primaryCurrency, false, format).ToString(true);
        }

        private string Format(decimal value)
        {
            return new Money(value, _primaryCurrency, false).ToString(true);
        }

        #endregion
    }
}
