using System.Net.Mime;
using Microsoft.AspNetCore.Routing;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.Pdf;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public class OrderController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly OrderHelper _orderHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly IProviderManager _providerManager;
        private readonly IPdfConverter _pdfConverter;
        private readonly OrderSettings _orderSettings;

        public OrderController(
            SmartDbContext db,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            IProductAttributeMaterializer productAttributeMaterializer,
            OrderHelper orderHelper,
            IDateTimeHelper dateTimeHelper,
            IProviderManager providerManager,
            ProductUrlHelper productUrlHelper,
            IPdfConverter pdfConverter,
            OrderSettings orderSettings)
        {
            _db = db;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _orderHelper = orderHelper;
            _dateTimeHelper = dateTimeHelper;
            _providerManager = providerManager;
            _productUrlHelper = productUrlHelper;
            _pdfConverter = pdfConverter;
            _orderSettings = orderSettings;
        }

        [RequireSsl]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(x => x.ShippingAddress)
                .Include(x => x.BillingAddress)
                .Include(x => x.Shipments)
                .FindByIdAsync(id, false);

            if (await IsNonExistentOrderAsync(order))
                return NotFound();

            if (await IsUnauthorizedOrderAsync(order))
                return new UnauthorizedResult();

            var model = await _orderHelper.PrepareOrderDetailsModelAsync(order);

            return View(model);
        }

        [RequireSsl]
        public async Task<IActionResult> Print(int id, bool pdf = false)
        {
            var order = await _db.Orders
                .Include(x => x.ShippingAddress)
                .Include(x => x.BillingAddress)
                .Include(x => x.Shipments)
                .FindByIdAsync(id, false);

            if (await IsNonExistentOrderAsync(order))
                return NotFound();

            if (await IsUnauthorizedOrderAsync(order))
                return new UnauthorizedResult();

            var model = await _orderHelper.PrepareOrderDetailsModelAsync(order);
            var fileName = T("Order.PdfInvoiceFileName", order.Id);

            return await PrintCore(new List<OrderDetailsModel> { model }, pdf, fileName);
        }

        [AuthorizeAdmin]
        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> PrintMany(string ids = null, bool pdf = false)
        {
            const int maxOrders = 500;
            IList<Order> orders = null;
            var totalCount = 0;

            var orderQuery = _db.Orders
                .IncludeBillingAddress()
                .IncludeShippingAddress()
                .AsNoTracking();

            if (ids != null)
            {
                orders = await orderQuery
                    .Where(x => ids.ToIntArray().Contains(x.Id))
                    .ToListAsync();

                totalCount = orders.Count;
            }
            else
            {
                totalCount = await orderQuery
                    .ApplyStandardFilter()
                    .ToPagedList(0, 1)
                    .GetTotalCountAsync();

                if (totalCount > 0 && totalCount <= maxOrders)
                {
                    orders = await orderQuery
                        .ApplyStandardFilter()
                        .ToPagedList(0, int.MaxValue)
                        .LoadAsync();
                }
            }

            if (totalCount == 0)
            {
                NotifyInfo(T("Admin.Common.ExportNoData"));
                return RedirectToReferrer();
            }

            if (totalCount > maxOrders)
            {
                NotifyWarning(T("Admin.Common.ExportToPdf.TooManyItems"));
                return RedirectToReferrer();
            }

            var listModel = await orders
                .SelectAwait(async x => await _orderHelper.PrepareOrderDetailsModelAsync(x))
                .AsyncToList();

            return await PrintCore(listModel, pdf, "orders.pdf");
        }

        private async Task<IActionResult> PrintCore(List<OrderDetailsModel> model, bool pdf, string pdfFileName)
        {
            ViewBag.PdfMode = pdf;
            var viewName = "Details.Print";

            if (pdf)
            {
                // TODO: (mc) this is bad for multi-document processing, where orders can originate from different stores.
                var storeId = model[0].StoreId;
                var routeValues = new RouteValueDictionary
                {
                    ["storeId"] = storeId,
                    ["lid"] = Services.WorkContext.WorkingLanguage.Id
                };
                var pdfSettings = Services.SettingFactory.LoadSettings<PdfSettings>(storeId);

                var conversionSettings = new PdfConversionSettings
                {
                    Size = pdfSettings.LetterPageSizeEnabled ? PdfPageSize.Letter : PdfPageSize.A4,
                    Margins = new PdfPageMargins { Top = 35, Bottom = 35 },
                    Header = _pdfConverter.CreateFileInput(Url.Action("ReceiptHeader", "Pdf", routeValues)),
                    Footer = _pdfConverter.CreateFileInput(Url.Action("ReceiptFooter", "Pdf", routeValues)),
                    Page = _pdfConverter.CreateHtmlInput(await InvokeViewAsync(viewName, model))
                };

                var output = await _pdfConverter.GeneratePdfAsync(conversionSettings);
                return File(output, MediaTypeNames.Application.Pdf, pdfFileName);
            }

            return View(viewName, model);
        }

        public async Task<IActionResult> ReOrder(int id)
        {
            var order = await _db.Orders
                .Include(x => x.OrderItems)
                .ThenInclude(x => x.Product)
                .FindByIdAsync(id);

            if (await IsNonExistentOrderAsync(order))
                return NotFound();

            if (await IsUnauthorizedOrderAsync(order))
                return new UnauthorizedResult();

            await _orderProcessingService.ReOrderAsync(order);

            return RedirectToRoute("ShoppingCart");
        }

        [HttpPost, ActionName("Details")]
        [FormValueRequired("repost-payment")]
        public async Task<IActionResult> RePostPayment(int id /* orderId */)
        {
            var order = await _db.Orders.FindByIdAsync(id);

            if (await IsNonExistentOrderAsync(order))
                return NotFound();

            if (await IsUnauthorizedOrderAsync(order))
                return new UnauthorizedResult();

            try
            {
                if (await _paymentService.CanRePostProcessPaymentAsync(order))
                {
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = order,
                        IsRePostProcessPayment = true
                    };

                    await _paymentService.PostProcessPaymentAsync(postProcessPaymentRequest);

                    if (postProcessPaymentRequest.RedirectUrl.HasValue())
                    {
                        return Redirect(postProcessPaymentRequest.RedirectUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Details), "Order", new { id = order.Id });
        }

        [RequireSsl]
        public async Task<IActionResult> ShipmentDetails(int id /* shipmentId */)
        {
            var shipment = await _db.Shipments.FindByIdAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            var order = shipment.Order;

            if (await IsNonExistentOrderAsync(order))
            {
                return NotFound();
            }

            if (await IsUnauthorizedOrderAsync(order))
            {
                return new UnauthorizedResult();
            }

            var model = await PrepareShipmentDetailsModelAsync(shipment);

            return View(model);
        }

        protected async Task<ShipmentDetailsModel> PrepareShipmentDetailsModelAsync(Shipment shipment)
        {
            Guard.NotNull(shipment, nameof(shipment));

            var order = shipment.Order;
            if (order == null)
            {
                throw new Exception(T("Order.NotFound", shipment.OrderId));
            }

            var currentStore = Services.StoreContext.CurrentStore;
            var store = currentStore.Id != order.StoreId
                ? (await _db.Stores.FindByIdAsync(order.StoreId, false) ?? currentStore)
                : currentStore;
            var settingFactory = Services.SettingFactory;
            var catalogSettings = await settingFactory.LoadSettingsAsync<CatalogSettings>(store.Id);
            var shippingSettings = await settingFactory.LoadSettingsAsync<ShippingSettings>(store.Id);

            var model = new ShipmentDetailsModel
            {
                Id = shipment.Id,
                TrackingNumber = shipment.TrackingNumber,
                TrackingNumberUrl = shipment.TrackingUrl
            };

            if (shipment.ShippedDateUtc.HasValue)
            {
                model.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
            }

            if (shipment.DeliveryDateUtc.HasValue)
            {
                model.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
            }

            var srcm = _providerManager.GetProvider<IShippingRateComputationMethod>(order.ShippingRateComputationMethodSystemName, store.Id);

            if (srcm != null && srcm.IsShippingRateComputationMethodActive(shippingSettings))
            {
                var shipmentTracker = srcm.Value.ShipmentTracker;
                if (shipmentTracker != null)
                {
                    // The URL entered by the merchant takes precedence over an automatically generated URL.
                    if (model.TrackingNumberUrl.IsEmpty())
                    {
                        model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
                    }

                    if (shippingSettings.DisplayShipmentEventsToCustomers)
                    {
                        var shipmentEvents = await shipmentTracker.GetShipmentEventsAsync(shipment.TrackingNumber);
                        if (shipmentEvents != null)
                        {
                            foreach (var shipmentEvent in shipmentEvents)
                            {
                                var shipmentEventCountry = await _db.Countries
                                    .AsNoTracking()
                                    .ApplyIsoCodeFilter(shipmentEvent.CountryCode)
                                    .FirstOrDefaultAsync();

                                var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel
                                {
                                    Country = shipmentEventCountry != null ? shipmentEventCountry.GetLocalized(x => x.Name) : shipmentEvent.CountryCode,
                                    Date = shipmentEvent.Date,
                                    EventName = shipmentEvent.EventName,
                                    Location = shipmentEvent.Location
                                };

                                model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                            }
                        }
                    }
                }
            }

            // Products in this shipment.
            model.ShowSku = catalogSettings.ShowProductSku;

            foreach (var shipmentItem in shipment.ShipmentItems)
            {
                var orderItem = await _db.OrderItems
                    .Include(x => x.Product)
                    .FindByIdAsync(shipmentItem.OrderItemId, false);

                if (orderItem == null || orderItem.Product == null)
                    continue;

                var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(orderItem.Product.Id, orderItem.AttributeSelection);
                orderItem.Product.MergeWithCombination(attributeCombination);

                var shipmentItemModel = new ShipmentDetailsModel.ShipmentItemModel
                {
                    Id = shipmentItem.Id,
                    Sku = orderItem.Product.Sku,
                    ProductId = orderItem.Product.Id,
                    ProductName = orderItem.Product.GetLocalized(x => x.Name),
                    ProductSeName = await orderItem.Product.GetActiveSlugAsync(),
                    AttributeInfo = orderItem.AttributeDescription,
                    QuantityOrdered = orderItem.Quantity,
                    QuantityShipped = shipmentItem.Quantity
                };

                shipmentItemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(shipmentItemModel.ProductSeName, orderItem);

                model.Items.Add(shipmentItemModel);
            }

            model.Order = await _orderHelper.PrepareOrderDetailsModelAsync(order);
            return model;
        }

        private async Task<bool> IsNonExistentOrderAsync(Order order)
        {
            var result = order == null || order.Deleted;

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
            {
                result = result || (order.StoreId != 0 && order.StoreId != Services.StoreContext.CurrentStore.Id);

                if (_orderSettings.DisplayOrdersOfAllStores)
                {
                    result = false;
                }
            }

            return result;
        }

        private async Task<bool> IsUnauthorizedOrderAsync(Order order)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Order.Read))
                return order == null || order.CustomerId != Services.WorkContext.CurrentCustomer.Id;
            else
                return order == null;
        }
    }
}
