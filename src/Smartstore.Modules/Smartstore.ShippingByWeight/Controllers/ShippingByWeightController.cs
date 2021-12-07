using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.ShippingByWeight.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.ShippingByWeight.Controllers
{
    [Area("Admin")]
    public class ShippingByWeightController : ModuleController
    {
        private readonly SmartDbContext _db;
        private readonly IShippingService _shippingService;
        private readonly IProviderManager _providerManager;
        private readonly ShippingByWeightSettings _shippingByWeightSettings;
        private readonly MeasureSettings _measureSettings;

        public ShippingByWeightController(
            SmartDbContext db,
            IShippingService shippingService,
            IProviderManager providerManager,
            ShippingByWeightSettings shippingByWeightSettings,
            MeasureSettings measureSettings)
        {
            _db = db;
            _shippingService = shippingService;
            _providerManager = providerManager;
            _shippingByWeightSettings = shippingByWeightSettings;
            _measureSettings = measureSettings;
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
            
            var baseWeighMeasure = await _db.MeasureWeights.Where(x => x.Id == _measureSettings.BaseWeightId).FirstOrDefaultAsync();
            ViewBag.BaseWeightIn = baseWeighMeasure?.GetLocalized(x => x.Name) ?? string.Empty;
            ViewBag.PrimaryStoreCurrencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems();
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

            ViewBag.Provider = _providerManager.GetProvider("Smartstore.ShippingByWeight").Metadata;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Configure));
        }

        [LoadSetting]
        public async Task<IActionResult> Configure(ShippingByWeightSettings settings)
        {
            if (!await _db.ShippingMethods.AnyAsync())
            {
                NotifyWarning(T("Admin.Configuration.Shipping.Methods.NoMethodsLoaded"));
            }

            var model = MiniMapper.Map<ShippingByWeightSettings, ByWeightListModel>(settings);

            await PrepareViewBagAsync();

            return View(model);
        }

        [HttpPost, SaveSetting]
        public IActionResult Configure(ByWeightListModel model, ShippingByWeightSettings settings)
        {
            MiniMapper.Map(model, settings);

            NotifySuccess(T("Admin.Configuration.Updated"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> ByWeightList(GridCommand command)
        {
            var shippingRates = await _db.ShippingRatesByWeight()
                .AsNoTracking()
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var shippingMethods = await _db.ShippingMethods.ToDictionaryAsync(x => x.Id, x => x);
            var countries = await _db.Countries.ToDictionaryAsync(x => x.Id, x => x);

            var shippingRateModels = shippingRates.Select(x =>
            {
                var store = Services.StoreContext.GetStoreById(x.StoreId);
                var shippingMethod = shippingMethods.Get(x.ShippingMethodId);
                var country = countries.Get(x.CountryId);
                
                var m = new ByWeightModel
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    ShippingMethodId = x.ShippingMethodId,
                    CountryId = x.CountryId,
                    Zip = x.Zip.HasValue() ? x.Zip : "*",
                    From = x.From,
                    To = x.To,
                    UsePercentage = x.UsePercentage,
                    ShippingChargePercentage = x.ShippingChargePercentage,
                    ShippingChargeAmount = x.ShippingChargeAmount,
                    SmallQuantitySurcharge = x.SmallQuantitySurcharge,
                    SmallQuantityThreshold = x.SmallQuantityThreshold,
                    StoreName = store == null ? "*" : store.Name,
                    ShippingMethodName = shippingMethod?.Name ?? StringExtensions.NotAvailable,
                    CountryName = country?.Name ?? "*"
                };

                return m;
            })
            .ToList();

            var gridModel = new GridModel<ByWeightModel>
            {
                Rows = shippingRateModels,
                Total = shippingRates.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public async Task<IActionResult> ByWeightUpdate(ByWeightModel model)
        {
            var success = false;
            var shippingRate = await _db.ShippingRatesByWeight().FindByIdAsync(model.Id);

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
        public async Task<IActionResult> ByWeightDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var ShippingRates = await _db.ShippingRatesByWeight().GetManyAsync(ids, true);

                _db.ShippingRatesByWeight().RemoveRange(ShippingRates);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Create)]
        public async Task<IActionResult> AddShippingRateByWeight(ByWeightModel model)
        {
            var entity = MiniMapper.Map<ByWeightModel, ShippingRateByWeight>(model);
            entity.ShippingChargePercentage = model.UsePercentage ? model.ShippingChargePercentage : 0;
            entity.ShippingChargeAmount = model.UsePercentage ? 0 : model.ShippingChargeAmount;

            _db.ShippingRatesByWeight().Add(entity);

            await _db.SaveChangesAsync();

            NotifySuccess(T("Plugins.Shipping.ByWeight.AddNewRecord.Success"));

            return Json(new { Result = true });
        }
    }
}
