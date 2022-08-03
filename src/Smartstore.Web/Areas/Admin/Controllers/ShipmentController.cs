using System.Data;
using System.Linq.Dynamic.Core;
using System.Net.Mime;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Smartstore.Admin.Models.Orders;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Pdf;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ShipmentController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IAddressService _addressService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPdfConverter _pdfConverter;
        private readonly PdfSettings _pdfSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly CatalogSettings _catalogSettings;

        public ShipmentController(
            SmartDbContext db,
            IAddressService addressService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IOrderProcessingService orderProcessingService,
            IPdfConverter pdfConverter,
            PdfSettings pdfSettings,
            MeasureSettings measureSettings,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _addressService = addressService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _orderProcessingService = orderProcessingService;
            _pdfConverter = pdfConverter;
            _pdfSettings = pdfSettings;
            _measureSettings = measureSettings;
            _catalogSettings = catalogSettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Order.Read)]
        public IActionResult List()
        {
            PrepareViewBag();

            return View(new ShipmentListModel());
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> ShipmentList(GridCommand command, ShipmentListModel model)
        {
            var dtHelper = Services.DateTimeHelper;

            DateTime? startDate = model.StartDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.StartDate.Value, dtHelper.CurrentTimeZone);

            DateTime? endDate = model.EndDate == null
                ? null
                : dtHelper.ConvertToUtcTime(model.EndDate.Value, dtHelper.CurrentTimeZone).AddDays(1);

            var query = _db.Shipments.AsNoTracking();

            if (model.TrackingNumber.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.TrackingNumber, model.TrackingNumber);
            }

            if (model.ShippingMethod.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Order.ShippingMethod, model.ShippingMethod);
            }

            if (model.OrderId.GetValueOrDefault() > 0)
            {
                query = query.Where(x => x.OrderId == model.OrderId.Value);
            }

            var shipments = await query
                .Include(x => x.Order)
                .Where(x => x.Order != null)
                .ApplyTimeFilter(startDate, endDate)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await shipments.SelectAwait(async x =>
            {
                var m = new ShipmentModel();
                await PrepareShipmentModel(m, x, false);
                return m;
            })
            .AsyncToList();

            return Json(new GridModel<ShipmentModel>
            {
                Rows = rows,
                Total = shipments.TotalCount
            });
        }

        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> Create(int orderId)
        {
            var order = await _db.Orders
                .IncludeCustomer(true)
                .IncludeOrderItems()
                .IncludeShipments()
                .FindByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var model = new ShipmentModel
            {
                OrderId = order.Id,
            };

            var orderItemIds = order.OrderItems.ToDistinctArray(x => x.Id);
            if (orderItemIds.Any())
            {
                var baseWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
                var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);

                foreach (var orderItem in order.OrderItems)
                {
                    // We can ship only shippable products.
                    if (!orderItem.Product.IsShippingEnabled)
                        continue;

                    // Ensure that this product can be added to a shipment.
                    if (await _orderProcessingService.GetShippableItemsCountAsync(orderItem) <= 0)
                        continue;

                    var itemModel = await CreateShipmentItemModel(null, orderItem, baseDimension, baseWeight);
                    if (itemModel != null)
                    {
                        model.Items.Add(itemModel);
                    }
                }
            }

            PrepareViewBag();

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> Create(ShipmentModel model, IFormCollection form, bool continueEditing)
        {
            var order = await _db.Orders
                .IncludeCustomer(true)
                .IncludeOrderItems()
                .IncludeShipments()
                .FindByIdAsync(model.OrderId);

            if (order == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var quantities = new Dictionary<int, int>();

                foreach (var orderItem in order.OrderItems)
                {
                    foreach (var key in form.Keys)
                    {
                        if (key.EqualsNoCase($"qtyToAdd{orderItem.Id}"))
                        {
                            quantities.Add(orderItem.Id, form[key].FirstOrDefault().ToInt());
                            break;
                        }
                    }
                }

                var shipment = await _orderProcessingService.AddShipmentAsync(order, model.TrackingNumber, model.TrackingUrl, quantities);
                if (shipment != null)
                {
                    Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), order.GetOrderNumber());
                    NotifySuccess(T("Admin.Orders.Shipments.Added"));

                    return continueEditing
                       ? RedirectToAction(nameof(Edit), new { id = shipment.Id })
                       : RedirectToAction(nameof(OrderController.Edit), "Order", new { id = order.Id });
                }
                else
                {
                    NotifyError(T("Admin.Orders.Shipments.NoProductsSelected"));

                    return RedirectToAction(nameof(Create), new { orderId = order.Id });
                }
            }

            return await Create(order.Id);
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var shipment = await _db.Shipments
                .Include(x => x.ShipmentItems)
                .Include(x => x.Order.ShippingAddress.Country)
                .Include(x => x.Order.ShippingAddress.StateProvince)
                .FindByIdAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            var model = new ShipmentModel();
            await PrepareShipmentModel(model, shipment, true);
            PrepareViewBag();

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> Edit(ShipmentModel model, bool continueEditing)
        {
            var shipment = await _db.Shipments
                .Include(x => x.ShipmentItems)
                .Include(x => x.Order.ShippingAddress.Country)
                .Include(x => x.Order.ShippingAddress.StateProvince)
                .FindByIdAsync(model.Id);

            if (shipment == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                shipment.TrackingNumber = model.TrackingNumber;
                shipment.TrackingUrl = model.TrackingUrl;

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), shipment.Order.GetOrderNumber());

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = shipment.Id })
                    : RedirectToAction(nameof(OrderController.Edit), "Order", new { id = shipment.OrderId });
            }

            await PrepareShipmentModel(model, shipment, true);
            PrepareViewBag();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> Delete(int id)
        {
            var shipment = await _db.Shipments
                .Include(x => x.Order)
                .FindByIdAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            var orderId = shipment.OrderId;
            var orderNumber = shipment.Order.GetOrderNumber();

            _db.Shipments.Remove(shipment);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), orderNumber);
            NotifySuccess(T("Admin.Orders.Shipments.Deleted"));

            return RedirectToAction(nameof(OrderController.Edit), "Order", new { id = orderId });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> SetAsShipped(int id)
        {
            var shipment = await _db.Shipments
                .Include(x => x.Order)
                .FindByIdAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.ShipAsync(shipment, true);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), shipment.Order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [Permission(Permissions.Order.EditShipment)]
        public async Task<IActionResult> SetAsDelivered(int id)
        {
            var shipment = await _db.Shipments
                .Include(x => x.Order)
                .FindByIdAsync(id);

            if (shipment == null)
            {
                return NotFound();
            }

            try
            {
                await _orderProcessingService.DeliverAsync(shipment, true);

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditOrder, T("ActivityLog.EditOrder"), shipment.Order.GetOrderNumber());
            }
            catch (Exception ex)
            {
                NotifyError(ex, true);
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        [Permission(Permissions.Order.Read)]
        public async Task<IActionResult> PdfPackagingSlips(string selectedIds, bool all)
        {
            var ids = selectedIds.ToIntArray();
            if (!all && !ids.Any())
            {
                NotifyInfo(T("Admin.Common.ExportNoData"));
                return RedirectToReferrer();
            }

            var query = _db.Shipments
                .Include(x => x.ShipmentItems)
                .Include(x => x.Order.ShippingAddress.Country)
                .Include(x => x.Order.ShippingAddress.StateProvince)
                .AsQueryable();

            var expectedShipments = all
                ? await query.CountAsync()
                : ids.Length;

            if (expectedShipments > 500)
            {
                NotifyWarning(T("Admin.Common.ExportToPdf.TooManyItems"));
                return RedirectToReferrer();
            }

            if (!all)
            {
                query = query.Where(x => ids.Contains(x.Id));
            }

            var shipments = await query
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToListAsync();

            if (shipments.Count == 0)
            {
                NotifyInfo(T("Admin.Common.ExportNoData"));
                return RedirectToReferrer();
            }

            var pdfFileName = shipments.Count == 1
                ? $"PackagingSlip-{shipments[0].Id}.pdf"
                : "PackagingSlips.pdf";

            var models = await shipments
                .SelectAwait(async x =>
                {
                    var m = new ShipmentModel();
                    await PrepareShipmentModel(m, x, true);
                    return m;
                })
                .AsyncToList();

            // TODO: (mc) this is bad for multi-document processing, where orders can originate from different stores.
            var storeId = models[0].StoreId;
            var pdfSettings = await Services.SettingFactory.LoadSettingsAsync<PdfSettings>(storeId);
            var routeValues = new RouteValueDictionary(new
            {
                storeId,
                lid = Services.WorkContext.WorkingLanguage.Id,
                area = string.Empty
            });

            PrepareViewBag();

            var conversionSettings = new PdfConversionSettings
            {
                Size = pdfSettings.LetterPageSizeEnabled ? PdfPageSize.Letter : PdfPageSize.A4,
                Margins = new PdfPageMargins { Top = 35, Bottom = 35 },
                Page = _pdfConverter.CreateHtmlInput(await InvokeViewAsync("PdfPackagingSlips.Print", models)),
                Header = _pdfConverter.CreateFileInput(Url.Action("ReceiptHeader", "Pdf", routeValues)),
                Footer = _pdfConverter.CreateFileInput(Url.Action("ReceiptFooter", "Pdf", routeValues))
            };

            var output = await _pdfConverter.GeneratePdfAsync(conversionSettings);

            return File(output, MediaTypeNames.Application.Pdf, pdfFileName);
        }

        #region Utilities

        private async Task PrepareShipmentModel(ShipmentModel model, Shipment shipment, bool forEdit)
        {
            // Requires: Shipment.Order
            // Requires for edit: Shipment.Order, Shipment.Order.ShippingAddress, Shipment.ShipmentItems
            MiniMapper.Map(shipment, model);

            var order = shipment.Order;
            var baseWeight = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);

            model.StoreId = order.StoreId;
            model.LanguageId = order.CustomerLanguageId;
            model.OrderNumber = order.GetOrderNumber();
            model.PurchaseOrderNumber = order.PurchaseOrderNumber;
            model.ShippingMethod = order.ShippingMethod;
            model.TotalWeightString = shipment.TotalWeight.HasValue
                ? "{0:F2} [{1}]".FormatInvariant(shipment.TotalWeight, baseWeight?.GetLocalized(x => x.Name) ?? string.Empty)
                : string.Empty;
            model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(shipment.CreatedOnUtc, DateTimeKind.Utc);

            model.CanShip = !shipment.ShippedDateUtc.HasValue;
            model.CanDeliver = shipment.ShippedDateUtc.HasValue && !shipment.DeliveryDateUtc.HasValue;
            model.ShippedDate = shipment.ShippedDateUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                : null;
            model.DeliveryDate = shipment.DeliveryDateUtc.HasValue
                ? Services.DateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                : null;

            model.EditUrl = Url.Action(nameof(Edit), "Shipment", new { id = shipment.Id, area = "Admin" });
            model.OrderEditUrl = Url.Action(nameof(OrderController.Edit), "Order", new { id = shipment.OrderId, area = "Admin" });

            if (forEdit)
            {
                var store = Services.StoreContext.GetStoreById(order.StoreId) ?? Services.StoreContext.CurrentStore;

                model.MerchantCompanyInfo = await Services.SettingFactory.LoadSettingsAsync<CompanyInformationSettings>(store.Id);
                model.FormattedMerchantAddress = await _addressService.FormatAddressAsync(model.MerchantCompanyInfo, true);

                if (order.ShippingAddressId.HasValue)
                {
                    await order.ShippingAddress.MapAsync(model.ShippingAddress);
                }

                // Shipment items.
                var orderItemIds = shipment.ShipmentItems.ToDistinctArray(x => x.OrderItemId);
                if (orderItemIds.Any())
                {
                    var baseDimension = await _db.MeasureDimensions.FindByIdAsync(_measureSettings.BaseDimensionId, false);

                    var orderItems = await _db.OrderItems
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Include(x => x.Product)
                        .Include(x => x.Order)
                        .ThenInclude(x => x.Shipments)
                        .ThenInclude(x => x.ShipmentItems)
                        .Where(x => orderItemIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id);

                    model.Items = await shipment.ShipmentItems
                        .SelectAwait(async x => await CreateShipmentItemModel(x, orderItems.Get(x.OrderItemId), baseDimension, baseWeight))
                        .Where(x => x != null)
                        .AsyncToList();
                }
            }
        }

        private async Task<ShipmentModel.ShipmentItemModel> CreateShipmentItemModel(
            ShipmentItem shipmentItem,
            OrderItem orderItem,
            MeasureDimension baseDimension,
            MeasureWeight baseWeight)
        {
            // Requires: OrderItem.Product, OrderItem.Order, OrderItem.Order.Shipments, OrderItem.Order.Shipments.ShipmentItems
            if (orderItem == null || orderItem.Product == null)
            {
                return null;
            }

            var product = orderItem.Product;
            await _productAttributeMaterializer.MergeWithCombinationAsync(product, orderItem.AttributeSelection);

            var model = new ShipmentModel.ShipmentItemModel
            {
                Id = shipmentItem?.Id ?? 0,
                OrderItemId = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = product.Name,
                ProductType = product.ProductType,
                ProductTypeName = product.GetProductTypeLabel(Services.Localization),
                ProductTypeLabelHint = product.ProductTypeLabelHint,
                Sku = product.Sku,
                Gtin = product.Gtin,
                AttributeInfo = orderItem.AttributeDescription,
                ItemWeight = orderItem.ItemWeight.HasValue
                    ? "{0:F2} [{1}]".FormatInvariant(orderItem.ItemWeight, baseWeight?.GetLocalized(x => x.Name) ?? string.Empty)
                    : string.Empty,
                ItemDimensions = "{0:F2} x {1:F2} x {2:F2} [{3}]".FormatInvariant(
                    product.Length, product.Width, product.Height, baseDimension?.GetLocalized(x => x.Name) ?? string.Empty),
                QuantityOrdered = orderItem.Quantity,
                QuantityInThisShipment = shipmentItem?.Quantity ?? 0,
                QuantityInAllShipments = await _orderProcessingService.GetShipmentItemsCountAsync(orderItem),
                QuantityToAdd = await _orderProcessingService.GetShippableItemsCountAsync(orderItem)
            };

            if (product.ProductType == ProductType.BundledProduct && orderItem.BundleData.HasValue())
            {
                var bundleData = orderItem.GetBundleData();

                model.BundlePerItemPricing = product.BundlePerItemPricing;
                model.BundlePerItemShoppingCart = bundleData.Any(x => x.PerItemShoppingCart);

                model.BundleItems = bundleData
                    .Select(x => new ShipmentModel.BundleItemModel
                    {
                        Sku = x.Sku,
                        ProductName = x.ProductName,
                        ProductSeName = x.ProductSeName,
                        VisibleIndividually = x.VisibleIndividually,
                        Quantity = x.Quantity,
                        DisplayOrder = x.DisplayOrder,
                        AttributeInfo = x.AttributesInfo
                    })
                    .ToList();
            }

            return model;
        }

        private void PrepareViewBag()
        {
            ViewBag.DisplayPdfPackagingSlip = _pdfSettings.Enabled;
            ViewBag.ShowSku = _catalogSettings.ShowProductSku;
        }

        #endregion
    }
}
