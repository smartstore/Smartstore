using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CountryController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;

        public CountryController(
            SmartDbContext db,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
        }

        #region Utilities 

        private async Task UpdateLocalesAsync(Country country, CountryModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(country, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        private async Task UpdateLocalesAsync(StateProvince stateProvince, StateProvinceModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(stateProvince, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        private async Task PrepareCountryModelAsync(CountryModel model, Country country)
        {
            Guard.NotNull(model, nameof(model));

            if (country != null)
            {
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(country);
                model.NumberOfStates = country.StateProvinces.Count;
            }

            var currencies = await _db.Currencies
                .AsNoTracking()
                .ApplyStandardFilter(false)
                .ToListAsync();

            ViewBag.Currencies = currencies
                .Select(x => new SelectListItem { Text = x.GetLocalized(y => y.Name), Value = x.Id.ToString() })
                .ToList();
        }

        #endregion

        #region Countries

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Country.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Read)]
        public async Task<IActionResult> CountryList(GridCommand command)
        {
            var countries = await _db.Countries
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<Country, CountryModel>();
            var countryModels = await countries
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "Country", new { id = x.Id });

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<CountryModel>
            {
                Rows = countryModels,
                Total = await countries.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> CountryUpdate(CountryModel model)
        {
            var success = false;
            var country = await _db.Countries.FindByIdAsync(model.Id);

            if (country != null)
            {
                await MapperFactory.MapAsync(model, country);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _db.Countries.FindByIdAsync(id);
            if (country == null)
            {
                return RedirectToAction(nameof(List));
            }

            // Don't delete associated countries.
            if (!await _db.Addresses.Where(x => x.CountryId == id).AnyAsync())
            {
                _db.Countries.Remove(country);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Countries.Deleted"));
                return RedirectToAction(nameof(List));
            }
            else
            {
                NotifyError(T("Admin.Configuration.Countries.CannotDeleteDueToAssociatedAddresses"));
                return RedirectToAction(nameof(Edit), new { id = id });
            }
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Delete)]
        public async Task<IActionResult> CountryDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var countries = await _db.Countries.GetManyAsync(ids, true);
                var triedToDeleteAssociated = false;

                foreach (var country in countries)
                {
                    if (await _db.Addresses.Where(x => x.CountryId == country.Id).AnyAsync())
                    {
                        triedToDeleteAssociated = true;
                        NotifyError(T("Admin.Configuration.Countries.CannotDeleteDueToAssociatedAddresses"));
                    }
                    else
                    {
                        _db.Countries.Remove(country);
                    }
                }

                numDeleted = await _db.SaveChangesAsync();

                if (triedToDeleteAssociated && numDeleted == 0)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [Permission(Permissions.Configuration.Country.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new CountryModel();

            AddLocales(model.Locales);
            await PrepareCountryModelAsync(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Country.Create)]
        public async Task<IActionResult> Create(CountryModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var country = await MapperFactory.MapAsync<CountryModel, Country>(model);
                _db.Countries.Add(country);
                await _db.SaveChangesAsync();

                await UpdateLocalesAsync(country, model);
                await _storeMappingService.ApplyStoreMappingsAsync(country, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, country, form));
                NotifySuccess(T("Admin.Configuration.Countries.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = country.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareCountryModelAsync(model, null);

            return View(model);
        }

        [Permission(Permissions.Configuration.Country.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var country = await _db.Countries
                .Include(x => x.StateProvinces)
                .FindByIdAsync(id, false);

            if (country == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Country, CountryModel>(country);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = country.GetLocalized(x => x.Name, languageId, false, false);
            });

            await PrepareCountryModelAsync(model, country);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> Edit(CountryModel model, bool continueEditing, IFormCollection form)
        {
            var country = await _db.Countries
                .Include(x => x.StateProvinces)
                .FindByIdAsync(model.Id);

            if (country == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, country);
                await UpdateLocalesAsync(country, model);
                await _storeMappingService.ApplyStoreMappingsAsync(country, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, country, form));
                NotifySuccess(T("Admin.Configuration.Countries.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = country.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareCountryModelAsync(model, country);

            return View(model);
        }

        #endregion

        #region States / provinces

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Read)]
        public async Task<IActionResult> States(int countryId, GridCommand command)
        {
            var stateProvinces = await _db.StateProvinces
                .Where(x => x.CountryId == countryId)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<StateProvince, StateProvinceModel>();
            var stateProvinceModels = await stateProvinces
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            var gridModel = new GridModel<StateProvinceModel>
            {
                Rows = stateProvinceModels,
                Total = await stateProvinces.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(Permissions.Configuration.Country.Update)]
        public IActionResult StateCreatePopup(int countryId, string btnId, string formId)
        {
            var model = new StateProvinceModel
            {
                CountryId = countryId
            };

            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> StateCreatePopup(StateProvinceModel model, string btnId, string formId)
        {
            var country = await _db.Countries.FindByIdAsync(model.CountryId);
            if (country == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var stateProvince = await MapperFactory.MapAsync<StateProvinceModel, StateProvince>(model);
                _db.StateProvinces.Add(stateProvince);
                await _db.SaveChangesAsync();

                await UpdateLocalesAsync(stateProvince, model);

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Country.Read)]
        public async Task<IActionResult> StateEditPopup(int id, string btnId, string formId)
        {
            var stateProvince = await _db.StateProvinces.FindByIdAsync(id);
            if (stateProvince == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<StateProvince, StateProvinceModel>(stateProvince);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = stateProvince.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> StateEditPopup(StateProvinceModel model, string btnId, string formId)
        {
            var stateProvince = await _db.StateProvinces.FindByIdAsync(model.Id);
            if (stateProvince == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                await MapperFactory.MapAsync(model, stateProvince);
                await UpdateLocalesAsync(stateProvince, model);
                await _db.SaveChangesAsync();

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> StateUpdate(StateProvinceModel model)
        {
            var success = false;
            var stateProvinces = await _db.StateProvinces.FindByIdAsync(model.Id);

            if (stateProvinces != null)
            {
                await MapperFactory.MapAsync(model, stateProvinces);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(Permissions.Configuration.Country.Update)]
        public async Task<IActionResult> StateDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var stateProvinces = await _db.StateProvinces.GetManyAsync(ids, true);

                _db.StateProvinces.RemoveRange(stateProvinces);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        /// <summary>
        /// Permission validation is not required here.
        /// This action method gets called via an AJAX request.
        /// </summary>
        public async Task<IActionResult> GetStatesByCountryId(string countryId, bool? addEmptyStateIfRequired, bool? addAsterisk)
        {
            var country = await _db.Countries.FindByIdAsync(countryId.ToInt());
            var states = new List<StateProvince>();
            if (country != null)
            {
                states = await _db.StateProvinces
                    .AsNoTracking()
                    .Where(x => x.CountryId == country.Id)
                    .ToListAsync();
            }

            var result = (from s in states select new { id = s.Id, name = s.Name }).ToList();

            if (addEmptyStateIfRequired.HasValue && addEmptyStateIfRequired.Value && result.Count == 0)
            {
                result.Insert(0, new { id = 0, name = T("Admin.Address.OtherNonUS").Value });
            }

            if (addAsterisk.HasValue && addAsterisk.Value)
            {
                result.Insert(0, new { id = 0, name = "*" });
            }

            return new JsonResult(result);
        }

        #endregion
    }
}
