using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Shipping.Settings;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Shipping.Controllers
{
    [Route("[area]/shippingbytotal/{action=index}/{id?}")]
    public class ByTotalController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ICurrencyService _currencyService;
        private readonly IProviderManager _providerManager;
        private readonly ShippingByTotalSettings _shippingByTotalSettings;

        public ByTotalController(
            SmartDbContext db,
            ICurrencyService currencyService,
            IProviderManager providerManager,
            ShippingByTotalSettings shippingByTotalSettings)
        {
            _db = db;
            _currencyService = currencyService;
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
                .ToListAsync();

            ViewBag.AvailableCountries = countries.ToSelectListItems();
            ViewBag.AvailableStores = Services.StoreContext.GetAllStores().ToSelectListItems();
            ViewBag.PrimaryStoreCurrencyCode = _currencyService.PrimaryCurrency.CurrencyCode;
            ViewBag.AvailableShippingMethods = shippingMethods.Values.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            })
            .ToList();

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

            var model = MiniMapper.Map<ShippingByTotalSettings, ByTotalListModel>(_shippingByTotalSettings);

            await PrepareViewBagAsync();

            return View(model);
        }

        [HttpPost, SaveSetting]
        public IActionResult Configure(ByTotalListModel model, ShippingByTotalSettings settings)
        {
            MiniMapper.Map(model, settings);

            NotifySuccess(T("Admin.Configuration.Updated"));

            return RedirectToAction(nameof(Configure));
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Read)]
        public async Task<IActionResult> ShippingRateByTotalList(GridCommand command)
        {
            var shippingRates = await _db.ShippingRatesByTotal()
                .AsNoTracking()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var stores = Services.StoreContext.GetAllStores().ToDictionary(x => x.Id);
            var shippingMethods = await _db.ShippingMethods.ToDictionaryAsync(x => x.Id, x => x);

            var countryIds = shippingRates
                .Where(x => x.CountryId.HasValue)
                .ToDistinctArray(x => x.CountryId.Value);

            var countries = countryIds.Any()
                ? await _db.Countries
                    .Include(x => x.StateProvinces)
                    .Where(x => countryIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x)
                : new Dictionary<int, Country>();

            var stateProvinces = countries.Values
                .SelectMany(x => x.StateProvinces)
                .ToDictionarySafe(x => x.Id, x => x);

            var shippingRateModels = shippingRates.Select(x =>
            {
                var store = stores.Get(x.StoreId);
                var shippingMethod = shippingMethods.Get(x.ShippingMethodId);
                var country = x.CountryId.HasValue ? countries.Get(x.CountryId.Value) : null;
                var stateProvince = x.StateProvinceId.HasValue ? stateProvinces.Get(x.StateProvinceId.Value) : null;

                var m = new ByTotalModel
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    ShippingMethodId = x.ShippingMethodId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip.NullEmpty() ?? "*",
                    From = x.From,
                    To = x.To,
                    UsePercentage = x.UsePercentage,
                    ShippingChargePercentage = x.ShippingChargePercentage,
                    ShippingChargeAmount = x.ShippingChargeAmount,
                    BaseCharge = x.BaseCharge,
                    MaxCharge = x.MaxCharge,
                    StoreName = store?.Name ?? "*",
                    ShippingMethodName = shippingMethod?.Name ?? StringExtensions.NotAvailable,
                    CountryName = country?.Name ?? "*",
                    StateProvinceName = stateProvince?.Name ?? "*"
                };

                return m;
            })
            .ToList();

            var gridModel = new GridModel<ByTotalModel>
            {
                Rows = shippingRateModels,
                Total = shippingRates.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Shipping.Update)]
        public async Task<IActionResult> ShippingRateByTotalUpdate(ByTotalModel model)
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
        public async Task<IActionResult> ShippingRateByTotalDelete(GridSelection selection)
        {
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