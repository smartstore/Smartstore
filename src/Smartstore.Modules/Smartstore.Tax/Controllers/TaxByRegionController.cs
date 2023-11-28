using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Tax.Controllers
{
    public class TaxByRegionController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;

        public TaxByRegionController(SmartDbContext db, ITaxService taxService)
        {
            _db = db;
            _taxService = taxService;
        }

        private async Task PrepareViewBagAsync()
        {
            var taxCategories = await _db.TaxCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var firstCountryId = _db.Countries.ApplyStandardFilter(true).Select(x => x.Id).FirstOrDefault();
            var stateProvincesOfFirstCountry = await _db.StateProvinces
                .AsNoTracking()
                .Where(x => x.CountryId == firstCountryId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.AvailableTaxCategories = taxCategories.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString()
            })
            .ToList();

            ViewBag.AvailableStates = stateProvincesOfFirstCountry.ToSelectListItems() ?? new List<SelectListItem>();
            ViewBag.AvailableStates.Insert(0, new SelectListItem { Text = "*", Value = "0" });

            ViewBag.Provider = _taxService.LoadTaxProviderBySystemName("Tax.CountryStateZip").Metadata;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Configure));
        }

        public async Task<IActionResult> Configure()
        {
            if (!await _db.TaxCategories.AnyAsync())
            {
                NotifyWarning(T("Plugins.Tax.CountryStateZip.NoTaxCategoriesFound"));
            }

            await PrepareViewBagAsync();

            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Read)]
        public async Task<IActionResult> TaxRateList(GridCommand command)
        {
            var taxRates = await _db.TaxRates()
                .OrderBy(x => x.CountryId)
                .ThenBy(x => x.StateProvinceId)
                .ThenBy(x => x.Zip)
                .ThenBy(x => x.TaxCategoryId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var unavailable = T("Common.Unavailable").Value;
            var taxCategories = await _db.TaxCategories.ToDictionaryAsync(x => x.Id, x => x);

            var countryIds = taxRates
                .Where(x => x.CountryId != 0)
                .ToDistinctArray(x => x.CountryId);

            var countries = countryIds.Any()
                ? await _db.Countries
                    .Include(x => x.StateProvinces)
                    .Where(x => countryIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x)
                : new Dictionary<int, Country>();

            var stateProvinces = countries.Values
                .SelectMany(x => x.StateProvinces)
                .ToDictionarySafe(x => x.Id, x => x);

            var taxRateModels = taxRates.Select(x =>
            {
                var taxCategory = taxCategories.Get(x.TaxCategoryId);
                var country = countries.Get(x.CountryId);
                var stateProvince = stateProvinces.Get(x.StateProvinceId);

                var m = new ByRegionTaxRateModel
                {
                    Id = x.Id,
                    TaxCategoryId = x.TaxCategoryId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip.HasValue() ? x.Zip : "*",
                    Percentage = x.Percentage,
                    TaxCategoryName = taxCategory?.Name.EmptyNull(),
                    CountryName = country?.Name ?? unavailable,
                    StateProvinceName = stateProvince?.Name ?? "*"
                };

                return m;
            })
            .ToList();

            var gridModel = new GridModel<ByRegionTaxRateModel>
            {
                Rows = taxRateModels,
                Total = taxRates.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Update)]
        public async Task<IActionResult> TaxRateUpdate(ByRegionTaxRateModel model)
        {
            var success = false;
            var taxRate = await _db.TaxRates().FindByIdAsync(model.Id);

            if (taxRate != null)
            {
                await MapperFactory.MapAsync(model, taxRate);

                taxRate.Zip = model.Zip == "*" ? null : model.Zip;
                taxRate.Percentage = model.Percentage;

                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Delete)]
        public async Task<IActionResult> TaxRateDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var taxRates = await _db.TaxRates().GetManyAsync(ids, true);

                _db.TaxRates().RemoveRange(taxRates);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Tax.Create)]
        public async Task<IActionResult> AddTaxByRegionRecord(ByRegionTaxRateModel model)
        {
            _db.TaxRates().Add(new()
            {
                TaxCategoryId = model.TaxCategoryId,
                CountryId = model.CountryId,
                StateProvinceId = model.StateProvinceId,
                Zip = model.Zip == "*" ? null : model.Zip,
                Percentage = model.Percentage
            });

            await _db.SaveChangesAsync();

            NotifySuccess(T("Plugins.Tax.CountryStateZip.AddNewRecord.Success"));

            return Json(new { Result = true });
        }
    }
}