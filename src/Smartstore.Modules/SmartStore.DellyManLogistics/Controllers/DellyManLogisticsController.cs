using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Engine.Modularity;
using Smartstore.Shipping;
using Smartstore.Shipping.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using SmartStore.DellyManLogistics.Models;

namespace SmartStore.DellyManLogistics.Controllers
{
    [Route("[area]/distanceshipping/{action=index}/{id?}")]
    public class DellyManLogisticsController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;
        private readonly MultiStoreSettingHelper _settingHelper;
        private readonly ILogger<DellyManLogisticsController> _logger;
        private readonly DellyManLogisticsSettings _dellyManLogisticsSettings;
        private readonly ICommonServices _services;

        public DellyManLogisticsController(SmartDbContext db,
            IProviderManager providerManager,
            MultiStoreSettingHelper settingHelper,
            ILogger<DellyManLogisticsController> logger,
            DellyManLogisticsSettings dellyManLogisticsSettings,
             ICommonServices services)
        {
            _db = db;
            _providerManager = providerManager;
            _settingHelper = settingHelper;
            _logger = logger;
            _dellyManLogisticsSettings = dellyManLogisticsSettings;
            _services = services;
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(DellyManLogisticsSettings settings)
        {
            ViewBag.Provider = _providerManager.GetProvider(DistanceRateProvider.SystemName).Metadata;

            var model = MiniMapper.Map<DellyManLogisticsSettings, ConfigurationModel>(settings);

            return View(model);

        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model, IFormCollection form)
        {
            var storeScope = GetActiveStoreScopeConfiguration();
            var settings = await Services.SettingFactory.LoadSettingsAsync<DellyManLogisticsSettings>(storeScope);

            if (!ModelState.IsValid)
            {
                return await Configure(settings);
            }

            ModelState.Clear();

            //model.PublicKey = model.PublicKey.TrimSafe();
            //model.PrivateKey = model.PrivateKey.TrimSafe();
            //model.BaseUrl = model.BaseUrl.TrimSafe();

            // settings = ((ISettings)settings).Clone() as PaystackSettings;
            MiniMapper.Map(model, settings);

            _settingHelper.Contextualize(storeScope);
            await _settingHelper.UpdateSettingsAsync(settings, form);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));

            return RedirectToAction(nameof(Configure));
        }


        [HttpPost]
        public async Task<ActionResult> OrderStatusCallback(DellyManWebhookBaseModel model)
        {
            try
            {

                var webhookString = model.ToString();
                Logger.Log(LogLevel.Information, null, webhookString, null);
                var settings = _dellyManLogisticsSettings;

                var shipment = _services.DbContext.Shipments.FirstOrDefault(s => s.TrackingNumber == model.Order.TrackingID);

                if (shipment != null)
                {
                    var order = _services.DbContext.Orders.FindById(shipment.OrderId);
                    switch (model.Order.OrderStatus)
                    {
                        case "PENDING":
                            order.OrderStatus = Smartstore.Core.Checkout.Orders.OrderStatus.Pending;
                            break;
                        case "ASSIGNED":
                            order.OrderStatus = Smartstore.Core.Checkout.Orders.OrderStatus.Processing;
                            break;
                        case "INTRANSIT":
                            order.OrderStatus = Smartstore.Core.Checkout.Orders.OrderStatus.Processing;
                            shipment.ShippedDateUtc = DateTime.TryParse(model.Order.PickedUpAt, out DateTime pickedUpAt) ? pickedUpAt : default(DateTime?);
                            break;
                        case "COMPLETED":
                            order.OrderStatus = Smartstore.Core.Checkout.Orders.OrderStatus.Complete;
                            shipment.DeliveryDateUtc = DateTime.TryParse(model.Order.DeliveredAt, out DateTime deliveredAT) ? deliveredAT : default(DateTime?);
                            break;
                        case "CANCELLED":
                            order.OrderStatus = Smartstore.Core.Checkout.Orders.OrderStatus.Cancelled;
                            break;
                        default:
                            break;
                    }

                    _services.DbContext.Shipments.Update(shipment);
                    _services.DbContext.Orders.Update(order);

                    await _services.DbContext.SaveChangesAsync();
                }

                return new StatusCodeResult(200);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex, null, null);
                return new StatusCodeResult(500);
            }
        }


    }
}
