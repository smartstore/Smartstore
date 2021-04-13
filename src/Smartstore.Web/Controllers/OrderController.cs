using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public class OrderController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly OrderHelper _orderHelper;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly IProviderManager _providerManager;

        public OrderController(
            SmartDbContext db,
            IProductAttributeMaterializer productAttributeMaterializer,
            OrderHelper orderHelper, 
            IDateTimeHelper dateTimeHelper, 
            IProviderManager providerManager,
            ProductUrlHelper productUrlHelper)
        {
            _db = db;
            _productAttributeMaterializer = productAttributeMaterializer;
            _orderHelper = orderHelper;
            _dateTimeHelper = dateTimeHelper;
            _providerManager = providerManager;
            _productUrlHelper = productUrlHelper;
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

        /// <param name="id"><see cref="Shipment.Id"/></param>
        [RequireSsl]
        public async Task<IActionResult> ShipmentDetails(int id)
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

        [NonAction]
        protected async Task<ShipmentDetailsModel> PrepareShipmentDetailsModelAsync(Shipment shipment)
        {
            Guard.NotNull(shipment, nameof(shipment));

            var order = shipment.Order;
            if (order == null)
            {
                throw new SmartException(T("Order.NotFound", shipment.OrderId));
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
                    .FindByIdAsync(shipmentItem.OrderItemId);
                    
                if (orderItem == null)
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
