using System.Data;
using System.Linq.Dynamic.Core;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Admin.Models.Orders;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;
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
        private readonly Lazy<IPriceCalculationService> _priceCalculationService;
        private readonly Lazy<IProductService> _productService;
        private readonly Lazy<IShoppingCartValidator> _shoppingCartValidator;
        private readonly Lazy<IProductAttributeFormatter> _productAttributeFormatter;
        private readonly Lazy<IGiftCardService> _giftCardService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;
        private readonly IEncryptor _encryptor;
        private readonly ModuleManager _moduleManager;
        private readonly IMessageFactory _messageFactory;
        private readonly CatalogSettings _catalogSettings;
        private readonly TaxSettings _taxSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly PdfSettings _pdfSettings;
        private readonly SearchSettings _searchSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly Currency _primaryCurrency;

        public OrderController(
            SmartDbContext db,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            Lazy<IPriceCalculationService> priceCalculationService,
            Lazy<IProductService> productService,
            Lazy<IShoppingCartValidator> shoppingCartValidator,
            Lazy<IProductAttributeFormatter> productAttributeFormatter,
            Lazy<IGiftCardService> giftCardService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IEncryptor encryptor,
            ModuleManager moduleManager,
            IMessageFactory messageFactory,
            CatalogSettings catalogSettings,
            TaxSettings taxSettings,
            MeasureSettings measureSettings,
            PdfSettings pdfSettings,
            SearchSettings searchSettings,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings,
            AdminAreaSettings adminAreaSettings)
        {
            _db = db;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
            _giftCardService = giftCardService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _paymentService = paymentService;
            _taxService = taxService;
            _encryptor = encryptor;
            _moduleManager = moduleManager;
            _messageFactory = messageFactory;
            _catalogSettings = catalogSettings;
            _taxSettings = taxSettings;
            _measureSettings = measureSettings;
            _pdfSettings = pdfSettings;
            _searchSettings = searchSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;
            _adminAreaSettings = adminAreaSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> List()
        {
            var allPaymentProviders = await _paymentService.LoadAllPaymentProvidersAsync();

            var paymentProviders = allPaymentProviders
                .Select(x => new SelectListItem
                {
                    Text = (_moduleManager.GetLocalizedFriendlyName(x.Metadata).NullEmpty() ?? x.Metadata.FriendlyName.NullEmpty() ?? x.Metadata.SystemName).EmptyNull(),
                    Value = x.Metadata.SystemName
                })
                .ToList();

            var paymentProvidersCounts = paymentProviders
                .GroupBy(x => x.Text)
                .Select(x => new { Name = x.Key.EmptyNull(), Count = x.Count() })
                .ToDictionarySafe(x => x.Name, x => x.Count);

            // Append system name if there are payment methods with the same friendly name.
            paymentProviders = paymentProviders
                .OrderBy(x => x.Text)
                .Select(x =>
                {
                    if (paymentProvidersCounts.TryGetValue(x.Text, out var count) && count > 1)
                    {
                        x.Text = $"{x.Text} ({x.Value})";
                    }

                    return x;
                })
                .ToList();

            ViewBag.PaymentMethods = paymentProviders;
            ViewBag.Stores = Services.StoreContext.GetAllStores().ToSelectListItems();
            ViewBag.HideProfitReport = false;

            return View(new OrderListModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> OrderList(GridCommand command, OrderListModel model, int? productId = null, int? customerId = null)
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
                .Include(x => x.BillingAddress)
                .IncludeCustomer()
                .AsNoTracking()
                .ApplyAuditDateFilter(startDateUtc, endDateUtc)
                .ApplyStatusFilter(model.OrderStatusIds, model.PaymentStatusIds, model.ShippingStatusIds)
                .ApplyPaymentFilter(paymentMethodSystemnames);

            if (productId > 0)
            {
                orderQuery = orderQuery.Where(x => x.OrderItems.Any(i => i.ProductId == productId.Value));
            }
            if (customerId > 0)
            {
                orderQuery = orderQuery.Where(x => x.CustomerId == customerId.Value);
            }

            if (model.CustomerEmail.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilterFor(x => x.BillingAddress.Email, model.CustomerEmail);
            }
            if (model.CustomerName.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilter(
                    model.CustomerName,
                    LogicalRuleOperator.Or,
                    x => x.BillingAddress.FirstName,
                    x => x.BillingAddress.LastName);
            }
            if (model.OrderNumber.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilterFor(x => x.OrderNumber, model.OrderNumber);
            }
            if (model.PaymentId.HasValue())
            {
                orderQuery = orderQuery.ApplySearchFilter(
                    model.PaymentId,
                    LogicalRuleOperator.Or,
                    x => x.CaptureTransactionId,
                    x => x.AuthorizationTransactionId,
                    x => x.AuthorizationTransactionCode);
            }
            if (model.StoreId > 0)
            {
                orderQuery = orderQuery.Where(x => x.StoreId == model.StoreId);
            }

            orderQuery = orderQuery
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command);

            var orders = await orderQuery
                .ToPagedList(command)
                .LoadAsync();

            var paymentMethods = await orders
                .Where(x => x.PaymentMethodSystemName.HasValue())
                .Select(x => x.PaymentMethodSystemName)
                .Distinct()
                .SelectAwait(async x => await _paymentService.LoadPaymentProviderBySystemNameAsync(x))
                .AsyncToList();

            var paymentMethodsDic = paymentMethods
                .Where(x => x != null)
                .ToDictionarySafe(
                    x => x.Metadata.SystemName,
                    x => _moduleManager.GetLocalizedFriendlyName(x.Metadata),
                    StringComparer.OrdinalIgnoreCase);

            var rows = orders.Select(x =>
            {
                paymentMethodsDic.TryGetValue(x.PaymentMethodSystemName, out var paymentMethod);

                var shipTo = x.ShippingAddress;
                var m = new OrderOverviewModel();

                PrepareOrderOverviewModel(m, x);

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
            .ToList();

            var summaryQuery =
                from q in orderQuery
                group q by 1 into grp
                select new OrderAverageReportLine
                {
                    OrderCount = grp.Count(),
                    SumTax = grp.Sum(x => x.OrderTax),
                    SumOrderTotal = grp.Sum(x => x.OrderTotal)
                };

            var summary = await summaryQuery
                .OrderByDescending(x => x.SumOrderTotal)
                .FirstOrDefaultAsync() ?? new OrderAverageReportLine();

            var productCost = await orderQuery.GetOrdersProductCostsAsync();
            var profit = summary.SumOrderTotal - summary.SumTax - productCost;

            return Json(new GridModel<OrderOverviewModel>
            {
                Rows = rows,
                Total = orders.TotalCount,
                Aggregates = new
                {
                    profit = Services.CurrencyService.CreateMoney(profit, _primaryCurrency).ToString(true),
                    tax = Services.CurrencyService.CreateMoney(summary.SumTax, _primaryCurrency).ToString(true),
                    total = Services.CurrencyService.CreateMoney(summary.SumOrderTotal, _primaryCurrency).ToString(true)
                }
            });
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-order-by-number")]
        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> GoToOrder(OrderListModel model)
        {
            var orderId = 0;
            var orderNumber = model.GoDirectlyToNumber.TrimSafe();

            if (orderNumber.HasValue())
            {
                if (RegularExpressions.IsGuid.IsMatch(orderNumber))
                {
                    orderId = await _db.Orders
                        .Where(x => x.OrderGuid.ToString() == orderNumber)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    orderId = await _db.Orders
                        .Where(x => x.OrderNumber == orderNumber)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync();

                    if (orderId == 0 && int.TryParse(orderNumber, out orderId) && orderId > 0)
                    {
                        if (!await _db.Orders.AnyAsync(x => x.Id == orderId))
                        {
                            orderId = 0;
                        }
                    }
                }
            }

            if (orderId != 0)
            {
                return RedirectToAction(nameof(Edit), new { id = orderId });
            }

            NotifyWarning(T("Order.NotFound", orderNumber));

            return RedirectToAction(nameof(List));
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
        public async Task<IActionResult> ProcessOrder(string selectedIds, string operation)
        {
            var ids = selectedIds.ToIntArray();
            var orders = await _db.Orders
                .Include(x => x.Shipments)
                .IncludeCustomer(true)
                .IncludeOrderItems()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();

            if (orders.Count == 0 || operation.IsEmpty())
            {
                return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
            }

            const int maxErrors = 3;
            var numSuccess = 0;
            var numSkipped = 0;
            var numErrors = 0;
            var errorMessages = new HashSet<string>();
            var succeededOrderNumbers = new HashSet<string>();

            foreach (var o in orders)
            {
                try
                {
                    switch (operation)
                    {
                        case "cancel":
                            if (o.CanCancelOrder())
                            {
                                await _orderProcessingService.CancelOrderAsync(o, true);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "complete":
                            if (o.CanCompleteOrder())
                            {
                                await _orderProcessingService.CompleteOrderAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "ship":
                        case "deliver":
                            if (o.ShippingStatus != ShippingStatus.ShippingNotRequired && o.ShippingAddressId != 0)
                            {
                                var ship = operation == "ship";
                                if (o.Shipments.Count > 0)
                                {
                                    foreach (var shipment in o.Shipments)
                                    {
                                        if (ship && shipment.ShippedDateUtc == null)
                                        {
                                            await _orderProcessingService.ShipAsync(shipment, true);
                                        }
                                        else if (!ship && shipment.ShippedDateUtc != null && shipment.DeliveryDateUtc == null)
                                        {
                                            await _orderProcessingService.DeliverAsync(shipment, true);
                                        }
                                    }

                                    ++numSuccess;
                                    succeededOrderNumbers.Add(o.GetOrderNumber());
                                }
                                else
                                {
                                    if (ship)
                                    {
                                        var quantities = new Dictionary<int, int>();
                                        foreach (var orderItem in o.OrderItems)
                                        {
                                            quantities.Add(orderItem.Id, orderItem.Quantity);
                                        }

                                        var shipment = await _orderProcessingService.AddShipmentAsync(o, string.Empty, string.Empty, quantities);
                                        if (shipment != null)
                                        {
                                            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), o.GetOrderNumber());
                                        
                                            if (ship && shipment.ShippedDateUtc == null)
                                            {
                                                await _orderProcessingService.ShipAsync(shipment, true);
                                            }
                                    
                                            ++numSuccess;
                                            succeededOrderNumbers.Add(o.GetOrderNumber());
                                        }
                                        else
                                        {
                                            ++numSkipped;
                                        }
                                    }
                                    else
                                    {
                                        ++numSkipped;
                                    }
                                }
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "markpaid":
                            if (o.CanMarkOrderAsPaid())
                            {
                                await _orderProcessingService.MarkOrderAsPaidAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "capture":
                            if (await _orderProcessingService.CanCaptureAsync(o))
                            {
                                await _orderProcessingService.CaptureAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "refundoffline":
                            if (o.CanRefundOffline())
                            {
                                await _orderProcessingService.RefundOfflineAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "refund":
                            if (await _orderProcessingService.CanRefundAsync(o))
                            {
                                await _orderProcessingService.RefundAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "voidoffline":
                            if (o.CanVoidOffline())
                            {
                                await _orderProcessingService.VoidOfflineAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                        case "void":
                            if (await _orderProcessingService.CanVoidAsync(o))
                            {
                                await _orderProcessingService.VoidAsync(o);
                                ++numSuccess;
                                succeededOrderNumbers.Add(o.GetOrderNumber());
                            }
                            else
                            {
                                ++numSkipped;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errorMessages.Add(ex.Message);
                    if (++numErrors <= maxErrors)
                    {
                        Logger.Error(ex);
                    }
                }
            }

            var msg = new StringBuilder((errorMessages.Count * 100) + 100);
            msg.Append(T("Admin.Orders.ProcessingResult", numSuccess, ids.Length, numSkipped, numSkipped == 0 ? " class='hide'" : string.Empty));
            errorMessages.Take(maxErrors).Each(x => msg.Append($"<div class='text-danger mt-2'>{x}</div>"));

            NotifyInfo(msg.ToString());

            if (succeededOrderNumbers.Count > 0)
            {
                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), string.Join(", ", succeededOrderNumbers.OrderBy(x => x)));
            }

            return RedirectToReferrer(null, () => RedirectToAction(nameof(List)));
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

            return RedirectToAction(nameof(Edit), new { id });
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

            return RedirectToAction(nameof(Edit), new { id });
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
                await _orderProcessingService.CaptureAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id });
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

            return RedirectToAction(nameof(Edit), new { id });
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
                await _orderProcessingService.RefundAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id });
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

            return RedirectToAction(nameof(Edit), new { id });
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
                await _orderProcessingService.VoidAsync(order);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id });
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

            return RedirectToAction(nameof(Edit), new { id });
        }

        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> PartiallyRefundOrderPopup(int id, bool online)
        {
            var order = await _db.Orders.FindByIdAsync(id, false);
            if (order == null)
            {
                return NotFound();
            }

            var maxAmountToRefund = order.OrderTotal - order.RefundedAmount;

            var model = new OrderModel.RefundModel
            {
                Id = id,
                MaxAmountToRefund = maxAmountToRefund,
                MaxAmountToRefundString = Format(maxAmountToRefund)
            };

            ViewBag.Online = online;
            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> PartiallyRefundOrder(OrderModel.RefundModel model, bool online)
        {
            var success = false;

            try
            {
                var order = await _db.Orders.FindByIdAsync(model.Id);
                if (order != null)
                {
                    var maxAmountToRefund = order.OrderTotal - order.RefundedAmount;
                    var amountToRefund = model.AmountToRefund;

                    if (amountToRefund > maxAmountToRefund)
                    {
                        amountToRefund = maxAmountToRefund;
                    }

                    if (amountToRefund <= decimal.Zero)
                    {
                        NotifyError(T("Admin.OrderNotice.RefundAmountError"));
                    }
                    else if (online)
                    {
                        await _orderProcessingService.PartiallyRefundAsync(order, amountToRefund);
                        success = true;
                    }
                    else
                    {
                        await _orderProcessingService.PartiallyRefundOfflineAsync(order, amountToRefund);
                        success = true;
                    }

                    if (success)
                    {
                        Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                    }
                }
                else
                {
                    NotifyError(T("Order.NotFound", model.Id));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex, false);
            }

            return Json(new { success });
        }

        #endregion

        #region Edit, delete

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _db.Orders
                .IncludeCustomer(true)
                .IncludeOrderItems()
                .IncludeShipments()
                .IncludeGiftCardHistory()
                .IncludeBillingAddress()
                .FindByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderModel();
            await PrepareOrderModel(model, order);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Order.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var msg = T("ActivityLog.DeleteOrder", order.GetOrderNumber());

            await _orderProcessingService.DeleteOrderAsync(order);

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteOrder, msg);
            NotifySuccess(msg);

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Order.Read)]
        public IActionResult Print(int orderId, bool pdf = false)
        {
            return RedirectToAction("Print", "Order", new { id = orderId, pdf, area = string.Empty });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("btnSaveCC")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> EditCreditCardInfo(int id, EditOrderModel model)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.AllowStoringCreditCardNumber)
            {
                order.CardType = _encryptor.EncryptText(model.CardType);
                order.CardName = _encryptor.EncryptText(model.CardName);
                order.CardNumber = _encryptor.EncryptText(model.CardNumber);
                order.MaskedCreditCardNumber = _encryptor.EncryptText(_paymentService.GetMaskedCreditCardNumber(model.CardNumber));
                order.CardCvv2 = _encryptor.EncryptText(model.CardCvv2);
                order.CardExpirationMonth = _encryptor.EncryptText(model.CardExpirationMonth);
                order.CardExpirationYear = _encryptor.EncryptText(model.CardExpirationYear);

                await _db.SaveChangesAsync();
                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("btnSaveDD")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> EditDirectDebitInfo(int id, EditOrderModel model)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (order.AllowStoringDirectDebit)
            {
                order.DirectDebitAccountHolder = _encryptor.EncryptText(model.DirectDebitAccountHolder);
                order.DirectDebitAccountNumber = _encryptor.EncryptText(model.DirectDebitAccountNumber);
                order.DirectDebitBankCode = _encryptor.EncryptText(model.DirectDebitBankCode);
                order.DirectDebitBankName = _encryptor.EncryptText(model.DirectDebitBankName);
                order.DirectDebitBIC = _encryptor.EncryptText(model.DirectDebitBIC);
                order.DirectDebitCountry = _encryptor.EncryptText(model.DirectDebitCountry);
                order.DirectDebitIban = _encryptor.EncryptText(model.DirectDebitIban);

                await _db.SaveChangesAsync();
                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost, ActionName("Edit")]
        [FormValueRequired("btnSaveOrderTotals")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> EditOrderTotals(int id, EditOrderModel model)
        {
            var order = await _db.Orders.FindByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderSubtotalInclTax = model.OrderSubtotalInclTax;
            order.OrderSubtotalExclTax = model.OrderSubtotalExclTax;
            order.OrderSubTotalDiscountInclTax = model.OrderSubTotalDiscountInclTax;
            order.OrderSubTotalDiscountExclTax = model.OrderSubTotalDiscountExclTax;
            order.OrderShippingInclTax = model.OrderShippingInclTax;
            order.OrderShippingExclTax = model.OrderShippingExclTax;
            order.PaymentMethodAdditionalFeeInclTax = model.PaymentMethodAdditionalFeeInclTax;
            order.PaymentMethodAdditionalFeeExclTax = model.PaymentMethodAdditionalFeeExclTax;
            order.TaxRates = model.TaxRates;
            order.OrderTax = model.OrderTax;
            order.OrderDiscount = model.OrderDiscount;
            order.CreditBalance = model.CreditBalance;
            order.OrderTotalRounding = model.OrderTotalRounding;
            order.OrderTotal = model.OrderTotal;

            await _db.SaveChangesAsync();
            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> EditOrderItem(UpdateOrderItemModel model, IFormCollection form)
        {
            var orderItem = await _db.OrderItems
                .Include(x => x.Order)
                .FindByIdAsync(model.Id);

            if (orderItem == null)
            {
                return NotFound();
            }

            var context = new UpdateOrderDetailsContext
            {
                UpdateOrderItem = true,
                AdjustInventory = model.AdjustInventory,
                UpdateRewardPoints = model.UpdateRewardPoints,
                UpdateTotals = model.UpdateTotals,
                NewQuantity = model.NewQuantity ?? 0,
                NewUnitPriceInclTax = model.NewUnitPriceInclTax,
                NewUnitPriceExclTax = model.NewUnitPriceExclTax,
                NewTaxRate = model.NewTaxRate,
                NewDiscountInclTax = model.NewDiscountInclTax,
                NewDiscountExclTax = model.NewDiscountExclTax,
                NewPriceInclTax = model.NewPriceInclTax,
                NewPriceExclTax = model.NewPriceExclTax
            };

            // INFO: UpdateOrderDetailsAsync performs commit.
            await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), orderItem.Order.GetOrderNumber());
            TempData[UpdateOrderDetailsContext.InfoKey] = context.ToString(Services.Localization);

            return RedirectToAction(nameof(Edit), new { id = orderItem.OrderId });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> DeleteOrderItem(UpdateOrderItemModel model)
        {
            var orderItem = await _db.OrderItems
                .Include(x => x.Order)
                .Include(x => x.Product)
                .FindByIdAsync(model.Id);

            if (orderItem == null)
            {
                return NotFound();
            }

            if (orderItem.Product.IsGiftCard &&
                await _db.GiftCards.AnyAsync(x => x.PurchasedWithOrderItemId == orderItem.Id))
            {
                NotifyError(T("Admin.Orders.OrderItem.CannotDeleteAssociatedGiftCards"));

                return RedirectToAction(nameof(Edit), new { id = orderItem.OrderId });
            }

            var context = new UpdateOrderDetailsContext
            {
                NewQuantity = 0,
                AdjustInventory = model.AdjustInventory,
                UpdateRewardPoints = model.UpdateRewardPoints,
                UpdateTotals = model.UpdateTotals
            };

            await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

            var orderId = orderItem.OrderId;
            var orderNumber = orderItem.Order.GetOrderNumber();

            _db.OrderItems.Remove(orderItem);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), orderNumber);
            TempData[UpdateOrderDetailsContext.InfoKey] = context.ToString(Services.Localization);

            return RedirectToAction(nameof(Edit), new { id = orderId });
        }

        [Permission(Permissions.Order.ReturnRequest.Create)]
        public async Task<IActionResult> AddReturnRequest(int orderId, int orderItemId)
        {
            var order = await _db.Orders
                .AsSplitQuery()
                .Include(x => x.OrderItems)
                .Include(x => x.Customer)
                .ThenInclude(x => x.ReturnRequests)
                .FindByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems
                .Where(x => x.Id == orderItemId)
                .FirstOrDefault();

            if (orderItem == null)
            {
                return NotFound();
            }

            if (orderItem.Quantity > 0)
            {
                var returnRequest = new ReturnRequest
                {
                    StoreId = order.StoreId,
                    OrderItemId = orderItem.Id,
                    Quantity = orderItem.Quantity,
                    CustomerId = order.CustomerId,
                    ReasonForReturn = string.Empty,
                    RequestedAction = string.Empty,
                    StaffNotes = string.Empty,
                    ReturnRequestStatus = ReturnRequestStatus.Pending
                };

                order.Customer.ReturnRequests.Add(returnRequest);
                await _db.SaveChangesAsync();

                return RedirectToAction("Edit", "ReturnRequest", new { id = returnRequest.Id });
            }

            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> ResetDownloadCount(int orderId, int orderItemId)
        {
            var order = await _db.Orders
                .Include(x => x.OrderItems)
                .FindByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems
                .Where(x => x.Id == orderItemId)
                .FirstOrDefault();

            if (orderItem == null)
            {
                return NotFound();
            }

            orderItem.DownloadCount = 0;
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> ActivateDownloadOrderItem(int orderId, int orderItemId)
        {
            var order = await _db.Orders
                .Include(x => x.OrderItems)
                .FindByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems
                .Where(x => x.Id == orderItemId)
                .FirstOrDefault();

            if (orderItem == null)
            {
                return NotFound();
            }

            orderItem.IsDownloadActivated = !orderItem.IsDownloadActivated;
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> UploadLicenseFilePopup(string btnId, string formId, int id, int orderItemId)
        {
            var order = await _db.Orders
                .IncludeOrderItems()
                .FindByIdAsync(id, false);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
            {
                return NotFound();
            }

            if (!orderItem.Product.IsDownload)
            {
                throw new ArgumentException(T("Admin.Orders.Products.NotDownloadable"));
            }

            var model = new OrderModel.UploadLicenseModel
            {
                LicenseDownloadId = orderItem.LicenseDownloadId ?? 0,
                OldLicenseDownloadId = orderItem.LicenseDownloadId ?? 0,
                OrderId = order.Id,
                OrderItemId = orderItem.Id
            };

            return View(model);
        }

        [HttpPost]
        [FormValueRequired("uploadlicense")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> UploadLicenseFilePopup(string btnId, string formId, OrderModel.UploadLicenseModel model)
        {
            var order = await _db.Orders
                .IncludeOrderItems()
                .FindByIdAsync(model.OrderId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
            if (orderItem == null)
            {
                return NotFound();
            }

            var isUrlDownload = Request.Form["is-url-download-" + model.LicenseDownloadId] == "true";
            var setOldFileToTransient = false;

            if (model.LicenseDownloadId != model.OldLicenseDownloadId && model.LicenseDownloadId != 0 && !isUrlDownload)
            {
                // Insert download if a new file was uploaded.
                var mediaFileInfo = await Services.MediaService.GetFileByIdAsync(model.LicenseDownloadId);

                var download = new Download
                {
                    MediaFile = mediaFileInfo.File,
                    EntityId = model.OrderId,
                    EntityName = "LicenseDownloadId",
                    DownloadGuid = Guid.NewGuid(),
                    UseDownloadUrl = false,
                    DownloadUrl = string.Empty,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsTransient = false
                };

                _db.Downloads.Add(download);
                await _db.SaveChangesAsync();

                orderItem.LicenseDownloadId = download.Id;

                setOldFileToTransient = true;
            }
            else if (isUrlDownload)
            {
                var download = await _db.Downloads.FindByIdAsync(model.LicenseDownloadId);

                download.IsTransient = false;
                download.UpdatedOnUtc = DateTime.UtcNow;
                orderItem.LicenseDownloadId = model.LicenseDownloadId;

                setOldFileToTransient = true;
            }

            if (setOldFileToTransient && model.OldLicenseDownloadId > 0)
            {
                // Set old download to transient if LicenseDownloadId is 0.
                var oldDownload = await _db.Downloads.FindByIdAsync(model.OldLicenseDownloadId);
                oldDownload.IsTransient = true;
                oldDownload.UpdatedOnUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            ViewBag.RefreshPage = true;
            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost, ActionName("UploadLicenseFilePopup")]
        [FormValueRequired("deletelicense")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> DeleteLicenseFilePopup(string btnId, string formId, OrderModel.UploadLicenseModel model)
        {
            var order = await _db.Orders
                .IncludeOrderItems()
                .FindByIdAsync(model.OrderId);

            if (order == null)
            {
                return NotFound();
            }

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
            if (orderItem == null)
            {
                return NotFound();
            }

            // Set deleted file to transient.
            var download = await _db.Downloads.FindByIdAsync(model.OldLicenseDownloadId);
            download.IsTransient = true;

            // Detach license.
            orderItem.LicenseDownloadId = null;

            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            ViewBag.RefreshPage = true;
            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> AddressEdit(int addressId, int orderId)
        {
            if (!await _db.Orders.AnyAsync(x => x.Id == orderId))
            {
                return NotFound();
            }

            var address = await _db.Addresses
                .Include(x => x.Country)
                .Include(x => x.StateProvince)
                .FindByIdAsync(addressId);

            if (address == null)
            {
                return NotFound();
            }

            var model = new OrderAddressModel
            {
                OrderId = orderId
            };

            await address.MapAsync(model.Address);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> AddressEdit(OrderAddressModel model, bool continueEditing)
        {
            var order = await _db.Orders.FindByIdAsync(model.OrderId);
            if (order == null)
            {
                return NotFound();
            }

            var address = await _db.Addresses.FindByIdAsync(model.Address.Id);
            if (address == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                MiniMapper.Map(model.Address, address);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishOrderUpdatedAsync(order);
                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

                return continueEditing
                    ? RedirectToAction(nameof(AddressEdit), new { addressId = address.Id, orderId = order.Id })
                    : RedirectToAction(nameof(Edit), new { id = order.Id });
            }

            await address.MapAsync(model.Address);

            return View(model);
        }

        // INFO: shipment action methods moved to new ShipmentController and were renamed in some cases.
        #endregion

        #region Order notes

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> OrderNoteList(int orderId)
        {
            var order = await _db.Orders
                .Include(x => x.OrderNotes)
                .FindByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var rows = order.OrderNotes
                .OrderByDescending(x => x.CreatedOnUtc)
                .Select(x => new OrderModel.OrderNote
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    DisplayToCustomer = x.DisplayToCustomer,
                    Note = x.FormatOrderNoteText(),
                    CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                })
                .ToList();

            if (order.HasNewPaymentNotification)
            {
                order.HasNewPaymentNotification = false;
                await _db.SaveChangesAsync();
            }

            return Json(new GridModel<OrderModel.OrderNote>
            {
                Rows = rows,
                Total = order.OrderNotes.Count
            });
        }

        [HttpPost]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> OrderNoteInsert(int orderId, bool displayToCustomer, string message)
        {
            var order = await _db.Orders
                .Include(x => x.Customer)
                .FindByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            var orderNote = new OrderNote
            {
                DisplayToCustomer = displayToCustomer,
                Note = message,
                CreatedOnUtc = DateTime.UtcNow,
            };

            order.OrderNotes.Add(orderNote);
            await _db.SaveChangesAsync();

            if (displayToCustomer)
            {
                await _messageFactory.SendNewOrderNoteAddedCustomerNotificationAsync(orderNote, Services.WorkContext.WorkingLanguage.Id);
            }

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            return Json(new { Result = true });
        }

        [HttpPost]
        [Permission(Permissions.Order.Update)]
        public async Task<IActionResult> OrderNoteDelete(GridSelection selection, int orderId)
        {
            var success = false;
            var order = await _db.Orders
                .Include(x => x.OrderNotes)
                .FindByIdAsync(orderId);

            if (order != null)
            {
                var ids = selection.GetEntityIds().ToArray();
                var orderNotes = order.OrderNotes.Where(x => ids.Contains(x.Id));

                if (orderNotes.Any())
                {
                    _db.OrderNotes.RemoveRange(orderNotes);
                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

                    success = true;
                }
            }

            return Json(new { Success = success });
        }

        #endregion

        #region Add product to order

        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> AddProductToOrder(int orderId, int productId)
        {
            var order = await _db.Orders
                .Include(x => x.Customer)
                .FindByIdAsync(orderId);

            var product = await _db.Products
                .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductAttribute)
                .FindByIdAsync(productId);

            var model = new AddOrderProductModel();

            await PrepareAddOrderProductModel(model, product, order);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditItem)]
        public async Task<IActionResult> AddProductToOrder(AddOrderProductModel model, ProductVariantQuery query)
        {
            var order = await _db.Orders
                .IncludeCustomer()
                .IncludeOrderItems()
                .FindByIdAsync(model.OrderId);

            var product = await _db.Products
                .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.ProductAttribute)
                .Include(x => x.ProductVariantAttributes)
                .ThenInclude(x => x.RuleSet)
                .FindByIdAsync(model.ProductId);

            if (order == null || product == null)
            {
                return NotFound();
            }

            if (product.ProductType == ProductType.BundledProduct)
            {
                throw new NotSupportedException("Adding a product bundle to an existing order is not supported.");
            }

            var utcNow = DateTime.UtcNow;
            var warnings = new List<string>();
            var attributes = product.ProductVariantAttributes
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var giftCardInfo = product.IsGiftCard
                ? query.GetGiftCardInfo(product.Id, 0)
                : null;

            var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(query, attributes, product.Id, 0);
            if (giftCardInfo != null)
            {
                selection.AddGiftCardInfo(giftCardInfo);
            }

            await _shoppingCartValidator.Value.ValidateProductAttributesAsync(
                product,
                selection,
                order.StoreId,
                warnings,
                model.Quantity,
                order.Customer);

            _shoppingCartValidator.Value.ValidateGiftCardInfo(product, selection, warnings);

            if (warnings.Any())
            {
                await PrepareAddOrderProductModel(model, product, order);

                ViewBag.Warnings = warnings;

                return View(model);
            }

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, selection);

            var attributeDescription = await _productAttributeFormatter.Value.FormatAttributesAsync(selection, product, ProductAttributeFormatOptions.Default, order.Customer);
            var productCost = await _priceCalculationService.Value.CalculateProductCostAsync(product, selection);

            var displayDeliveryTime =
                _shoppingCartSettings.DeliveryTimesInShoppingCart != DeliveryTimesPresentation.None &&
                product.DeliveryTimeId.HasValue &&
                product.IsShippingEnabled &&
                product.DisplayDeliveryTimeAccordingToStock(_catalogSettings);

            var orderItem = new OrderItem
            {
                OrderItemGuid = Guid.NewGuid(),
                Order = order,
                ProductId = product.Id,
                Sku = product.Sku,
                UnitPriceInclTax = model.UnitPriceInclTax,
                UnitPriceExclTax = model.UnitPriceExclTax,
                PriceInclTax = model.PriceInclTax,
                PriceExclTax = model.PriceExclTax,
                TaxRate = model.TaxRate,
                AttributeDescription = attributeDescription,
                RawAttributes = selection.AsJson(),
                Quantity = model.Quantity,
                DiscountAmountInclTax = decimal.Zero,
                DiscountAmountExclTax = decimal.Zero,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = 0,
                ProductCost = productCost.Amount,
                DeliveryTimeId = product.GetDeliveryTimeIdAccordingToStock(_catalogSettings),
                DisplayDeliveryTime = displayDeliveryTime
            };

            order.OrderItems.Add(orderItem);

            if (product.IsGiftCard)
            {
                _db.GiftCards.AddRange(Enumerable.Repeat(new GiftCard
                {
                    GiftCardType = product.GiftCardType,
                    PurchasedWithOrderItem = orderItem,
                    Amount = model.UnitPriceExclTax,
                    IsGiftCardActivated = false,
                    GiftCardCouponCode = _giftCardService.Value.GenerateGiftCardCode(),
                    RecipientName = giftCardInfo.RecipientName,
                    RecipientEmail = giftCardInfo.RecipientEmail,
                    SenderName = giftCardInfo.SenderName,
                    SenderEmail = giftCardInfo.SenderEmail,
                    Message = giftCardInfo.Message,
                    IsRecipientNotified = false,
                    CreatedOnUtc = utcNow
                }, orderItem.Quantity));
            }

            await _db.SaveChangesAsync();

            if (model.AdjustInventory || model.UpdateTotals)
            {
                var context = new UpdateOrderDetailsContext
                {
                    OldQuantity = 0,
                    NewQuantity = orderItem.Quantity,
                    OldPriceInclTax = decimal.Zero,
                    OldPriceExclTax = decimal.Zero,
                    AdjustInventory = model.AdjustInventory,
                    UpdateTotals = model.UpdateTotals
                };

                await _orderProcessingService.UpdateOrderDetailsAsync(orderItem, context);

                TempData[UpdateOrderDetailsContext.InfoKey] = context.ToString(Services.Localization);
            }

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());

            return RedirectToAction(nameof(Edit), new { id = order.Id });
        }

        #endregion

        #region Reports

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> BestsellersReport()
        {
            var countries = await _db.Countries
                .AsNoTracking()
                .Where(x => x.AllowsBilling)
                .ToListAsync();

            var countryItems = countries
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();
            countryItems.Insert(0, new SelectListItem { Text = T("Admin.Address.SelectCountry"), Value = "0" });

            ViewBag.Countries = countryItems;
            ViewBag.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;

            return View(new BestsellersReportModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> BestsellersReportList(GridCommand command, BestsellersReportModel model)
        {
            var dtHelper = Services.DateTimeHelper;
            var sorting = ReportSorting.ByAmountDesc;

            if (command.Sorting?.Any() ?? false)
            {
                var sort = command.Sorting.First();
                if (sort.Member == nameof(BestsellersReportLineModel.TotalQuantity))
                {
                    sorting = sort.Descending
                        ? ReportSorting.ByQuantityDesc
                        : ReportSorting.ByQuantityAsc;
                }
                else if (sort.Member == nameof(BestsellersReportLineModel.TotalAmount))
                {
                    sorting = sort.Descending
                        ? ReportSorting.ByAmountDesc
                        : ReportSorting.ByAmountAsc;
                }
            }

            DateTime? startDate = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDate = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var orderStatusId = model.OrderStatusId == 0 ? null : new[] { model.OrderStatusId };
            var paymentStatusId = model.PaymentStatusId == 0 ? null : new[] { model.PaymentStatusId };
            var shippingStatusId = model.ShippingStatusId == 0 ? null : new[] { model.ShippingStatusId };
            var countryId = model.BillingCountryId == 0 ? (int?)null : model.BillingCountryId;

            var orderItemQuery = _db.OrderItems
                .AsNoTracking()
                .ApplyOrderFilter(0, startDate, endDate, orderStatusId, paymentStatusId, shippingStatusId, countryId)
                .ApplyProductFilter(null, true);

            var reportLines = await orderItemQuery
                .SelectAsBestsellersReportLine(sorting)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await reportLines.MapAsync(Services, true);

            return Json(new GridModel<BestsellersReportLineModel>
            {
                Rows = rows,
                Total = await reportLines.GetTotalCountAsync()
            });
        }

        [Permission(Permissions.Order.Read)]
        public IActionResult NeverSoldReport()
        {
            ViewBag.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;

            return View(new NeverSoldReportModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> NeverSoldReportList(GridCommand command, NeverSoldReportModel model)
        {
            var dtHelper = Services.DateTimeHelper;
            var groupedProductId = (int)ProductType.GroupedProduct;

            DateTime? startDate = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDate = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var subQuery =
                from oi in _db.OrderItems
                join o in _db.Orders on oi.OrderId equals o.Id
                where
                    (!startDate.HasValue || startDate.Value <= o.CreatedOnUtc) &&
                    (!endDate.HasValue || endDate.Value >= o.CreatedOnUtc)
                select oi.ProductId;

            var productQuery =
                from p in _db.Products.AsNoTracking()
                where !subQuery.Distinct().Contains(p.Id) && p.ProductTypeId != groupedProductId && !p.IsSystemProduct
                orderby p.Name
                select p;

            var products = await productQuery
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await products.MapAsync(Services.MediaService);

            return Json(new GridModel<ProductOverviewModel>
            {
                Rows = rows,
                Total = products.TotalCount
            });
        }

        #endregion

        #region Utilities

        private void PrepareOrderOverviewModel(OrderOverviewModel model, Order order)
        {
            MiniMapper.Map(order, model);

            model.OrderNumber = order.GetOrderNumber();
            model.StoreName = Services.StoreContext.GetStoreById(order.StoreId)?.Name ?? StringExtensions.NotAvailable;
            model.CustomerName = order.Customer?.GetDisplayName(T);
            model.CustomerEmail = order.BillingAddress?.Email ?? order.Customer?.FindEmail();
            model.OrderTotalString = Format(order.OrderTotal);
            model.OrderStatusString = Services.Localization.GetLocalizedEnum(order.OrderStatus);
            model.PaymentStatusString = Services.Localization.GetLocalizedEnum(order.PaymentStatus);
            model.ShippingStatusString = Services.Localization.GetLocalizedEnum(order.ShippingStatus);
            model.ShippingMethod = order.ShippingMethod.NaIfEmpty();
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(order.UpdatedOnUtc, DateTimeKind.Utc);
            model.EditUrl = Url.Action(nameof(Edit), "Order", new { id = order.Id });
            model.CustomerEditUrl = Url.Action("Edit", "Customer", new { id = order.CustomerId });
        }

        private async Task PrepareOrderModel(OrderModel model, Order order)
        {
            Guard.NotNull(model);
            Guard.NotNull(order);

            var language = Services.WorkContext.WorkingLanguage;
            var store = Services.StoreContext.GetStoreById(order.StoreId);
            var taxRates = order.TaxRatesDictionary;

            MiniMapper.Map(order, model);
            PrepareOrderOverviewModel(model, order);

            if (order.AffiliateId != 0)
            {
                var affiliate = await _db.Affiliates
                    .Include(x => x.Address)
                    .FindByIdAsync(order.AffiliateId);

                model.AffiliateFullName = affiliate?.Address?.GetFullName()?.NullEmpty() ?? StringExtensions.NotAvailable;
            }

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

            model.OrderShippingInclTaxString = Format(order.OrderShippingInclTax, true, null, PricingTarget.ShippingCharge);
            model.OrderShippingExclTaxString = Format(order.OrderShippingExclTax, false, null, PricingTarget.ShippingCharge);

            if (order.PaymentMethodAdditionalFeeInclTax != decimal.Zero)
            {
                model.PaymentMethodAdditionalFeeInclTaxString = Format(order.PaymentMethodAdditionalFeeInclTax, true, null, PricingTarget.PaymentFee);
                model.PaymentMethodAdditionalFeeExclTaxString = Format(order.PaymentMethodAdditionalFeeExclTax, false, null, PricingTarget.PaymentFee);
            }

            model.OrderTaxString = Format(order.OrderTax);
            model.TaxRatesList = taxRates
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
                    Amount = Format(-x.UsedValue),
                    GiftCardId = x.GiftCardId
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
                model.DirectDebitAccountHolder = _encryptor.DecryptText(order.DirectDebitAccountHolder);
                model.DirectDebitAccountNumber = _encryptor.DecryptText(order.DirectDebitAccountNumber);
                model.DirectDebitBankCode = _encryptor.DecryptText(order.DirectDebitBankCode);
                model.DirectDebitBankName = _encryptor.DecryptText(order.DirectDebitBankName);
                model.DirectDebitBIC = _encryptor.DecryptText(order.DirectDebitBIC);
                model.DirectDebitCountry = _encryptor.DecryptText(order.DirectDebitCountry);
                model.DirectDebitIban = _encryptor.DecryptText(order.DirectDebitIban);
            }

            var pm = await _paymentService.LoadPaymentProviderBySystemNameAsync(order.PaymentMethodSystemName);
            if (pm != null)
            {
                model.DisplayCompletePaymentNote = order.PaymentStatus == PaymentStatus.Pending && await pm.Value.CanRePostProcessPaymentAsync(order);
                model.PaymentMethod = _moduleManager.GetLocalizedFriendlyName(pm.Metadata);
            }
            if (model.PaymentMethod.IsEmpty())
            {
                model.PaymentMethod = order.PaymentMethodSystemName;
            }

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

            //model.MaxAmountToRefund = order.OrderTotal - order.RefundedAmount;
            //model.MaxAmountToRefundString = Format(model.MaxAmountToRefund);

            model.RecurringPaymentId = await _db.RecurringPayments
                .ApplyStandardFilter(order.Id, null, null, true)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (order.BillingAddress != null)
            {
                await order.BillingAddress.MapAsync(model.BillingAddress);
            }

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                var shipTo = order.ShippingAddress;
                if (shipTo != null)
                {
                    model.ShippingAddress = await shipTo.MapAsync();

                    var googleAddressQuery = $"{shipTo.Address1} {shipTo.ZipPostalCode} {shipTo.City} {shipTo.Country?.Name ?? string.Empty}";

                    model.ShippingAddressGoogleMapsUrl = Services.ApplicationContext.AppConfiguration.Google.MapsUrl.FormatInvariant(
                        language.UniqueSeoCode.EmptyNull().ToLower(),
                        googleAddressQuery.UrlEncode());
                }

                model.CanAddNewShipments = await _orderProcessingService.CanAddItemsToShipmentAsync(order);
            }

            // Purchase order number (we have to find a better to inject this information because it's related to a certain plugin).
            model.DisplayPurchaseOrderNumber = order.PaymentMethodSystemName.EqualsNoCase("Payments.PurchaseOrderNumber");
            model.CheckoutAttributeInfo = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(order.CheckoutAttributeDescription));
            model.HasDownloadableProducts = order.OrderItems.Any(x => x.Product.IsDownload);
            model.UpdateOrderItemInfo = TempData[UpdateOrderDetailsContext.InfoKey] as string;

            model.UpdateOrderItem = new UpdateOrderItemModel
            {
                Caption = T("Admin.Orders.EditOrderDetails"),
                ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending,
                UpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending,
                // UpdateRewardPoints only visible for unpending orders (see RewardPointsSettingsValidator).
                ShowUpdateRewardPoints = order.OrderStatusId > (int)OrderStatus.Pending && order.RewardPointsWereAdded,
                UpdateRewardPoints = order.RewardPointsWereAdded
            };

            model.Items = await CreateOrderItemsModels(order);

            ViewBag.DisplayPdfInvoice = _pdfSettings.Enabled;
            ViewBag.AllowCustomersToSelectTaxDisplayType = _taxSettings.AllowCustomersToSelectTaxDisplayType;
            ViewBag.TaxDisplayType = _taxSettings.TaxDisplayType;
            ViewBag.DisplayTaxRates = _taxSettings.DisplayTaxRates;
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();
            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;
        }

        private async Task PrepareAddOrderProductModel(AddOrderProductModel model, Product product, Order order)
        {
            if (product == null)
            {
                throw new ArgumentException(T("Products.NotFound", model.ProductId));
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var currency = await _db.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CurrencyCode == order.CustomerCurrencyCode) ?? _primaryCurrency;

            var calculationOptions = _priceCalculationService.Value.CreateDefaultOptions(false, customer, currency);
            calculationOptions.IgnoreDiscounts = true;

            var calculationContext = new PriceCalculationContext(product, calculationOptions);
            var unitPrice = await _priceCalculationService.Value.CalculatePriceAsync(calculationContext);
            var priceTax = unitPrice.Tax.Value;

            model.OrderId = order.Id;
            model.ProductId = product.Id;
            model.Name = product.GetLocalized(x => x.Name);
            model.ProductType = product.ProductType;
            model.UnitPriceInclTax = priceTax.PriceGross;
            model.UnitPriceExclTax = priceTax.PriceNet;
            model.PriceInclTax = priceTax.PriceGross;
            model.PriceExclTax = priceTax.PriceNet;
            model.TaxRate = priceTax.Rate.Rate;
            model.ShowUpdateTotals = order.OrderStatusId <= (int)OrderStatus.Pending;
            model.GiftCard.IsGiftCard = product.IsGiftCard;
            model.GiftCard.GiftCardType = product.GiftCardType;

            model.UpdateTotals = model.ShowUpdateTotals;

            var attributes = product.ProductVariantAttributes
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var linkedProducts = new Dictionary<int, Product>();
            var linkedProductIds = attributes
                .SelectMany(x => x.ProductVariantAttributeValues)
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .ToDistinctArray(x => x.LinkedProductId);

            if (linkedProductIds.Any())
            {
                linkedProducts = await _db.Products
                    .AsNoTracking()
                    .Where(x => linkedProductIds.Contains(x.Id) && x.Visibility != ProductVisibility.Hidden)
                    .ToDictionaryAsync(x => x.Id);
            }

            foreach (var attribute in attributes)
            {
                var attributeValues = attribute.IsListTypeAttribute()
                    ? attribute.ProductVariantAttributeValues.OrderBy(x => x.DisplayOrder).ToList()
                    : new List<ProductVariantAttributeValue>();

                var attributeModel = new AddOrderProductModel.ProductVariantAttributeModel
                {
                    Id = attribute.Id,
                    ProductId = attribute.ProductId,
                    BundleItemId = 0,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Alias = attribute.ProductAttribute.Alias,
                    Name = attribute.ProductAttribute.GetLocalized(x => x.Name),
                    Description = attribute.ProductAttribute.GetLocalized(x => x.Description),
                    TextPrompt = attribute.TextPrompt,
                    CustomData = attribute.CustomData,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType,
                    AllowedFileExtensions = _catalogSettings.FileUploadAllowedExtensions
                };

                if (attribute.IsListTypeAttribute())
                {
                    foreach (var value in attributeValues)
                    {
                        var valueModel = new AddOrderProductModel.ProductVariantAttributeValueModel
                        {
                            Id = value.Id,
                            PriceAdjustment = string.Empty,
                            Name = value.GetLocalized(x => x.Name),
                            Alias = value.Alias,
                            Color = value.Color,
                            IsPreSelected = value.IsPreSelected
                        };

                        if (value.ValueType == ProductVariantAttributeValueType.ProductLinkage &&
                            linkedProducts.TryGetValue(value.LinkedProductId, out var linkedProduct))
                        {
                            valueModel.SeName = await linkedProduct.GetActiveSlugAsync();
                        }

                        attributeModel.Values.Add(valueModel);
                    }
                }

                model.ProductVariantAttributes.Add(attributeModel);
            }

            ViewBag.PrimaryStoreCurrencyCode = _primaryCurrency.CurrencyCode;
        }

        private async Task<List<OrderModel.OrderItemModel>> CreateOrderItemsModels(Order order)
        {
            var result = new List<OrderModel.OrderItemModel>();
            var returnRequestsMap = new Multimap<int, ReturnRequest>();
            var giftCardIdsMap = new Multimap<int, int>();
            var orderItemIds = order.OrderItems.Select(x => x.Id).ToArray();

            if (orderItemIds.Any())
            {
                var returnRequests = await _db.ReturnRequests
                    .AsNoTracking()
                    .ApplyStandardFilter(orderItemIds)
                    .ToListAsync();

                var giftCards = await _db.GiftCards
                    .AsNoTracking()
                    .Where(x => x.PurchasedWithOrderItemId != null && orderItemIds.Contains(x.PurchasedWithOrderItemId.Value))
                    .OrderBy(x => x.Id)
                    .Select(x => new
                    {
                        x.Id,
                        OrderItemId = x.PurchasedWithOrderItemId.Value
                    })
                    .ToListAsync();

                returnRequestsMap = returnRequests.ToMultimap(x => x.OrderItemId, x => x);
                giftCardIdsMap = giftCards.ToMultimap(x => x.OrderItemId, x => x.Id);
            }

            foreach (var item in order.OrderItems)
            {
                var product = item.Product;
                await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

                var model = MiniMapper.Map<OrderItem, OrderModel.OrderItemModel>(item);
                model.ProductName = product.GetLocalized(x => x.Name);
                model.Sku = item.Sku.NullEmpty() ?? product.Sku;
                model.ProductType = product.ProductType;
                model.ProductTypeName = product.GetProductTypeLabel(Services.Localization);
                model.ProductTypeLabelHint = product.ProductTypeLabelHint;
                model.IsDownload = product.IsDownload;
                model.DownloadActivationType = product.DownloadActivationType;
                model.UnitPriceInclTaxString = Format(item.UnitPriceInclTax, true, true);
                model.UnitPriceExclTaxString = Format(item.UnitPriceExclTax, false, true);
                model.PriceInclTaxString = Format(item.PriceInclTax, true, true);
                model.PriceExclTaxString = Format(item.PriceExclTax, false, true);
                model.DiscountAmountInclTaxString = Format(item.DiscountAmountInclTax, true, true);
                model.DiscountAmountExclTaxString = Format(item.DiscountAmountExclTax, false, true);

                if (product.IsRecurring)
                {
                    var period = Services.Localization.GetLocalizedEnum(product.RecurringCyclePeriod);
                    model.RecurringInfo = T("Admin.Orders.Products.RecurringPeriod", product.RecurringCycleLength, period);
                }

                if (returnRequestsMap.ContainsKey(item.Id))
                {
                    model.ReturnRequests = returnRequestsMap[item.Id]
                        .Select(x => new OrderModel.ReturnRequestModel
                        {
                            Id = x.Id,
                            Quantity = x.Quantity,
                            Status = x.ReturnRequestStatus,
                            StatusString = Services.Localization.GetLocalizedEnum(x.ReturnRequestStatus)
                        })
                        .ToList();
                }

                if (giftCardIdsMap.ContainsKey(item.Id))
                {
                    model.PurchasedGiftCardIds = giftCardIdsMap[item.Id].ToList();
                }

                if (product.ProductType == ProductType.BundledProduct && item.BundleData.HasValue())
                {
                    var bundleData = item.GetBundleData();

                    model.BundlePerItemPricing = product.BundlePerItemPricing;
                    model.BundlePerItemShoppingCart = bundleData.Any(x => x.PerItemShoppingCart);
                    model.BundleItems = bundleData
                        .Select(x => new OrderModel.BundleItemModel
                        {
                            ProductId = x.ProductId,
                            Sku = x.Sku,
                            ProductName = x.ProductName,
                            ProductSeName = x.ProductSeName,
                            VisibleIndividually = x.VisibleIndividually,
                            Quantity = x.Quantity,
                            DisplayOrder = x.DisplayOrder,
                            AttributesInfo = x.AttributesInfo,
                            PriceWithDiscount = model.BundlePerItemShoppingCart
                                ? Format(x.PriceWithDiscount, false, true)
                                : null
                        })
                        .ToList();
                }

                result.Add(model);
            }

            return result;
        }

        private string Format(decimal value, bool priceIncludesTax, bool? displayTaxSuffix = null, PricingTarget target = PricingTarget.Product)
        {
            var format = _taxService.GetTaxFormat(displayTaxSuffix, priceIncludesTax, target, Services.WorkContext.WorkingLanguage);

            return new Money(value, _primaryCurrency, false, format).ToString(true);
        }

        private string Format(decimal value)
        {
            return new Money(value, _primaryCurrency, false).ToString(true);
        }

        #endregion
    }
}
