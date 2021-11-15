using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Shipping.Domain;
using Smartstore.Shipping.Models;
using Smartstore.Shipping.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Shipping.Controllers
{
    [Route("[area]/[controller]/{action=index}/{id?}")]
    public class ByTotalController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IShippingService _shippingService;
        private readonly IProviderManager _providerManager;
        private readonly ShippingByTotalSettings _shippingByTotalSettings;

        public ByTotalController(
            SmartDbContext db, 
            IShippingService shippingService, 
            IProviderManager providerManager, 
            ShippingByTotalSettings shippingByTotalSettings)
        {
            _db = db;
            _shippingService = shippingService;
            _providerManager = providerManager;
            _shippingByTotalSettings = shippingByTotalSettings;
        }

        private async Task PrepareViewBagAsync()
        {
            var shippingMethods = await _db.ShippingMethods
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToDictionaryAsync(x => x.Id);

            var stateProvinces = await _db.StateProvinces
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            var stateProvincesOfFirstCountry = stateProvinces.Values.Where(x => x.CountryId == countries.Values.FirstOrDefault().Id).ToList();

            ViewBag.AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems();
            ViewBag.PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;

            ViewBag.AvailableShippingMethods = shippingMethods.Values.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            })
            .ToList();

            ViewBag.AvailableCountries = countries.Values.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            })
            .ToList();

            ViewBag.AvailableStates = stateProvincesOfFirstCountry.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            })
            .ToList();
            ViewBag.AvailableStates.Insert(0, new SelectListItem { Text = "*", Value = "0" });

            ViewBag.Provider = _providerManager.GetProvider("Shipping.ByTotal").Metadata;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Configure));
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _db.ShippingMethods.AnyAsync())
            {
                NotifyWarning(T("Admin.Configuration.Shipping.Methods.NoMethodsLoaded"));
            }

            var model = new ByTotalListModel
            {
                LimitMethodsToCreated = _shippingByTotalSettings.LimitMethodsToCreated,
                SmallQuantityThreshold = _shippingByTotalSettings.SmallQuantityThreshold,
                SmallQuantitySurcharge = _shippingByTotalSettings.SmallQuantitySurcharge,
                CalculateTotalIncludingTax = _shippingByTotalSettings.CalculateTotalIncludingTax,
                PrimaryStoreCurrencyCode = Services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode
            };

            await PrepareViewBagAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ByTotalListModel model)
        {
            _shippingByTotalSettings.LimitMethodsToCreated = model.LimitMethodsToCreated;
            _shippingByTotalSettings.SmallQuantityThreshold = model.SmallQuantityThreshold;
            _shippingByTotalSettings.SmallQuantitySurcharge = model.SmallQuantitySurcharge;
            _shippingByTotalSettings.CalculateTotalIncludingTax = model.CalculateTotalIncludingTax;

            await Services.SettingFactory.SaveSettingsAsync(_shippingByTotalSettings);

            NotifySuccess(T("Admin.Configuration.Updated"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> ByTotalList(GridCommand command)
        {
            var shippingRates = await _db.ShippingRatesByTotal()
                .AsNoTracking()
                .Include(x => x.ShippingMethod)
                .Include(x => x.Country)
                .Include(x => x.StateProvince)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var shippingRateModels = shippingRates.Select(x =>
            {
                var store = Services.StoreContext.GetStoreById(x.StoreId);
                var m = new ByTotalModel
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    ShippingMethodId = x.ShippingMethodId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip.HasValue() ? x.Zip : "*",
                    From = x.From,
                    To = x.To,
                    UsePercentage = x.UsePercentage,
                    ShippingChargePercentage = x.ShippingChargePercentage,
                    ShippingChargeAmount = x.ShippingChargeAmount,
                    BaseCharge = x.BaseCharge,
                    MaxCharge = x.MaxCharge,
                    StoreName = store == null ? "*" : store.Name,
                    ShippingMethodName = x.ShippingMethod == null ? string.Empty.NaIfEmpty() : x.ShippingMethod.Name,
                    CountryName = x.Country == null ? "*" : x.Country.Name,
                    StateProvinceName = x.StateProvince == null ? "*" : x.StateProvince.Name
                };

                return m;
            })
            .ToList();

            var gridModel = new GridModel<ByTotalModel>
            {
                Rows = shippingRateModels,
                Total = await shippingRates.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public async Task<IActionResult> ByTotalUpdate(ByTotalModel model)
        {
            var success = false;

            var shippingRate = await _db.ShippingRatesByTotal().FindByIdAsync(model.Id);
            
            if (shippingRate != null)
            {
                await MapperFactory.MapAsync(model, shippingRate);
                shippingRate.Zip = model.Zip == "*" ? null : model.Zip;
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Delete)]
        public async Task<IActionResult> ByTotalDelete(GridSelection selection)
        {
            // TODO: (mh) (core) We still need the Delete button in edit template.
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var ShippingRates = await _db.ShippingRatesByTotal().GetManyAsync(ids, true);

                _db.ShippingRatesByTotal().RemoveRange(ShippingRates);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Create)]
        public async Task<IActionResult> AddShippingRateByTotal(ByTotalModel model)
        {
            var rate = MiniMapper.Map<ByTotalModel, ShippingRateByTotal>(model);
            rate.ShippingChargePercentage = model.UsePercentage ? model.ShippingChargePercentage : 0;
            rate.ShippingChargeAmount = model.UsePercentage ? 0 : model.ShippingChargeAmount;

            _db.ShippingRatesByTotal().Add(rate);

            await _db.SaveChangesAsync();

            NotifySuccess(T("Plugins.Shipping.ByTotal.AddNewRecord.Success"));

            return Json(new { Result = true });
        }
    }
}