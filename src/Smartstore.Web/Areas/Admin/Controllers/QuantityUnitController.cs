using System.Linq.Dynamic.Core;

using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class QuantityUnitController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public QuantityUnitController(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available quantity units. 
        /// </summary>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all quantity units as JSON.</returns>
        public async Task<IActionResult> AllQuantityUnits(string label, int selectedId)
        {
            var quantityUnits = await _db.QuantityUnits
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            if (label.HasValue())
            {
                quantityUnits.Insert(0, new QuantityUnit { Name = label, Id = 0 });
            }

            var data = quantityUnits
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.GetLocalized(x => x.Name).Value,
                    Selected = x.Id == selectedId
                })
                .ToList();

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Measure.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> QuantityUnitList(GridCommand command)
        {
            var quantityUnits = await _db.QuantityUnits
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<QuantityUnit, QuantityUnitModel>();
            var quantityUnitModels = await quantityUnits
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            var gridModel = new GridModel<QuantityUnitModel>
            {
                Rows = quantityUnitModels,
                Total = await quantityUnits.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public async Task<IActionResult> QuantityUnitDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var quantityUnits = await _db.QuantityUnits.GetManyAsync(ids, true);
                    _db.QuantityUnits.RemoveRange(quantityUnits);

                    numDeleted = await _db.SaveChangesAsync();
                    success = true;
                }
                catch (Exception ex)
                {
                    NotifyError(ex);
                }
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [Permission(Permissions.Configuration.Measure.Create)]
        public IActionResult CreateQuantityUnitPopup(string btnId, string formId)
        {
            var model = new QuantityUnitModel();
            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public async Task<IActionResult> CreateQuantityUnitPopup(QuantityUnitModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var quantityUnit = await MapperFactory.MapAsync<QuantityUnitModel, QuantityUnit>(model);
                    _db.QuantityUnits.Add(quantityUnit);
                    await _db.SaveChangesAsync();

                    await UpdateLocalesAsync(quantityUnit, model);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.QuantityUnit.Added"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> EditQuantityUnitPopup(int id, string btnId, string formId)
        {
            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(id, false);
            if (quantityUnit == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<QuantityUnit, QuantityUnitModel>(quantityUnit);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = quantityUnit.GetLocalized(x => x.Name, languageId, false, false);
                locale.NamePlural = quantityUnit.GetLocalized(x => x.NamePlural, languageId, false, false);
                locale.Description = quantityUnit.GetLocalized(x => x.Description, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> EditQuantityUnitPopup(QuantityUnitModel model, string btnId, string formId)
        {
            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(model.Id);
            if (quantityUnit == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await MapperFactory.MapAsync(model, quantityUnit);
                    await UpdateLocalesAsync(quantityUnit, model);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.QuantityUnits.Updated"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> SetDefault(int id)
        {
            Guard.NotZero(id, nameof(id));

            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(id);
            quantityUnit.IsDefault = true;
            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        private async Task UpdateLocalesAsync(QuantityUnit quantityUnit, QuantityUnitModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(quantityUnit, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(quantityUnit, x => x.NamePlural, localized.NamePlural, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(quantityUnit, x => x.Description, localized.Description, localized.LanguageId);
            }
        }
    }
}