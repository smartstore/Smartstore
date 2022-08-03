using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
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
        private readonly IProviderManager _providerManager;
        private readonly MeasureSettings _measureSettings;

        public ShippingByWeightController(
            SmartDbContext db,
            IProviderManager providerManager,
            MeasureSettings measureSettings)
        {
            _db = db;
            _providerManager = providerManager;
            _measureSettings = measureSettings;
        }

        private async Task PrepareViewBagAsync()
        {
            var shippingMethods = await _db.ShippingMethods
                .AsNoTracking()
                .ToListAsync();

            var countries = await _db.Countries
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToListAsync();

            var baseWeighMeasure = await _db.MeasureWeights.Where(x => x.Id == _measureSettings.BaseWeightId).FirstOrDefaultAsync();

            ViewBag.AvailableCountries = countries.ToSelectListItems();
            ViewBag.BaseWeightIn = baseWeighMeasure?.GetLocalized(x => x.Name) ?? string.Empty;
            ViewBag.PrimaryStoreCurrencyCode = Services.CurrencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems();
            ViewBag.AvailableShippingMethods = shippingMethods.Select(x => new SelectListItem
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

        [LoadSetting, AuthorizeAdmin]
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

        [HttpPost, SaveSetting, AuthorizeAdmin]
        public IActionResult Configure(ByWeightListModel model, ShippingByWeightSettings settings)
        {
            MiniMapper.Map(model, settings);

            NotifySuccess(T("Admin.Configuration.Updated"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read), AuthorizeAdmin]
        public async Task<IActionResult> ShippingRateByWeightList(GridCommand command)
        {
            var shippingRates = await _db.ShippingRatesByWeight()
                .AsNoTracking()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            var shippingMethods = await _db.ShippingMethods.ToDictionaryAsync(x => x.Id, x => x);
            var countries = await _db.Countries.ToDictionaryAsync(x => x.Id, x => x);

            var shippingRateModels = shippingRates.Select(x =>
            {
                var store = stores.Get(x.StoreId);
                var shippingMethod = shippingMethods.Get(x.ShippingMethodId);
                var country = countries.Get(x.CountryId);

                var m = new ByWeightModel
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    ShippingMethodId = x.ShippingMethodId,
                    CountryId = x.CountryId,
                    Zip = x.Zip.NullEmpty() ?? "*",
                    From = x.From,
                    To = x.To,
                    UsePercentage = x.UsePercentage,
                    ShippingChargePercentage = x.ShippingChargePercentage,
                    ShippingChargeAmount = x.ShippingChargeAmount,
                    SmallQuantitySurcharge = x.SmallQuantitySurcharge,
                    SmallQuantityThreshold = x.SmallQuantityThreshold,
                    StoreName = store?.Name ?? "*",
                    ShippingMethodName = shippingMethod?.Name ?? StringExtensions.NotAvailable,
                    CountryName = country?.Name ?? "*"
                };

                return m;
            })
            .ToList();

            var gridModel = new GridModel<ByWeightModel>
            {
                Rows = shippingRateModels,
                Total = await shippingRates.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Update), AuthorizeAdmin]
        public async Task<IActionResult> ShippingRateByWeightUpdate(ByWeightModel model)
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
        [Permission(Permissions.Configuration.Shipping.Delete), AuthorizeAdmin]
        public async Task<IActionResult> ShippingRateByWeightDelete(GridSelection selection)
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
        [Permission(Permissions.Configuration.Shipping.Create), AuthorizeAdmin]
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
