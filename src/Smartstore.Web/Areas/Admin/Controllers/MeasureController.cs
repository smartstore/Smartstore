using System.Linq.Dynamic.Core;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class MeasureController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly MeasureSettings _measureSettings;

        public MeasureController(SmartDbContext db, ILocalizedEntityService localizedEntityService, MeasureSettings measureSettings)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
            _measureSettings = measureSettings;
        }

        #region Weights

        [Permission(Permissions.Configuration.Measure.Read)]
        public IActionResult Weights()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> MeasureWeightList(GridCommand command)
        {
            var measureWeights = await _db.MeasureWeights
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<MeasureWeight, MeasureWeightModel>();
            var measureWeightModels = await measureWeights
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.IsPrimaryWeight = x.Id == _measureSettings.BaseWeightId;

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<MeasureWeightModel>
            {
                Rows = measureWeightModels,
                Total = await measureWeights.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> MeasureWeightUpdate(MeasureWeightModel model)
        {
            var success = false;
            var measureWeight = await _db.MeasureWeights.FindByIdAsync(model.Id);

            if (measureWeight != null)
            {
                if (model.IsPrimaryWeight && _measureSettings.BaseWeightId != measureWeight.Id)
                {
                    _measureSettings.BaseWeightId = measureWeight.Id;
                    await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                }

                await MapperFactory.MapAsync(model, measureWeight);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(Permissions.Configuration.Measure.Create)]
        public IActionResult CreateWeightPopup(string btnId, string formId)
        {
            var model = new MeasureWeightModel();
            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public async Task<IActionResult> CreateWeightPopup(MeasureWeightModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var measureWeight = await MapperFactory.MapAsync<MeasureWeightModel, MeasureWeight>(model);
                    _db.MeasureWeights.Add(measureWeight);
                    await _db.SaveChangesAsync();

                    await UpdateWeightLocalesAsync(measureWeight, model);

                    if (model.IsPrimaryWeight)
                    {
                        _measureSettings.BaseWeightId = measureWeight.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                    }

                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Entity.Added"));
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
        public async Task<IActionResult> EditWeightPopup(int id, string btnId, string formId)
        {
            var measureWeight = await _db.MeasureWeights.FindByIdAsync(id, false);
            if (measureWeight == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<MeasureWeight, MeasureWeightModel>(measureWeight);
            model.IsPrimaryWeight = measureWeight.Id == _measureSettings.BaseWeightId;

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = measureWeight.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> EditWeightPopup(MeasureWeightModel model, string btnId, string formId)
        {
            var measureWeight = await _db.MeasureWeights.FindByIdAsync(model.Id);
            if (measureWeight == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await MapperFactory.MapAsync(model, measureWeight);
                    await UpdateWeightLocalesAsync(measureWeight, model);

                    if (model.IsPrimaryWeight && _measureSettings.BaseWeightId != measureWeight.Id)
                    {
                        _measureSettings.BaseWeightId = measureWeight.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                    }

                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Entity.Updated"));
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
        public async Task<IActionResult> SetDefaultWeight(int id)
        {
            Guard.NotZero(id, nameof(id));

            _measureSettings.BaseWeightId = id;
            await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public async Task<IActionResult> MeasureWeightDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var measureWeights = await _db.MeasureWeights.GetManyAsync(ids, true);
                    _db.MeasureWeights.RemoveRange(measureWeights);

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

        [NonAction]
        private async Task UpdateWeightLocalesAsync(MeasureWeight measureWeight, MeasureWeightModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(measureWeight, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        #endregion

        #region Dimensions


        [Permission(Permissions.Configuration.Measure.Read)]
        public IActionResult Dimensions()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> MeasureDimensionList(GridCommand command)
        {
            var measureDimensions = await _db.MeasureDimensions
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<MeasureDimension, MeasureDimensionModel>();
            var measureDimensionModels = await measureDimensions.SelectAwait(async x =>
            {
                var model = await mapper.MapAsync(x);
                model.IsPrimaryDimension = x.Id == _measureSettings.BaseDimensionId;

                return model;
            })
            .AsyncToList();

            var gridModel = new GridModel<MeasureDimensionModel>
            {
                Rows = measureDimensionModels,
                Total = await measureDimensions.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> MeasureDimensionUpdate(MeasureDimensionModel model)
        {
            var success = false;
            var measureDimension = await _db.MeasureDimensions.FindByIdAsync(model.Id);

            if (measureDimension != null)
            {
                if (model.IsPrimaryDimension && _measureSettings.BaseDimensionId != measureDimension.Id)
                {
                    _measureSettings.BaseDimensionId = measureDimension.Id;
                    await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                }

                await MapperFactory.MapAsync(model, measureDimension);
                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { success });
        }

        [Permission(Permissions.Configuration.Measure.Create)]
        public IActionResult CreateDimensionPopup(string btnId, string formId)
        {
            var model = new MeasureDimensionModel();
            AddLocales(model.Locales);

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Create)]
        public async Task<IActionResult> CreateDimensionPopup(MeasureDimensionModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var measureDimension = await MapperFactory.MapAsync<MeasureDimensionModel, MeasureDimension>(model);
                    _db.MeasureDimensions.Add(measureDimension);
                    await _db.SaveChangesAsync();

                    await UpdateDimensionLocalesAsync(measureDimension, model);

                    if (model.IsPrimaryDimension)
                    {
                        _measureSettings.BaseDimensionId = measureDimension.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                    }

                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Entity.Added"));
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
        public async Task<IActionResult> EditDimensionPopup(int id, string btnId, string formId)
        {
            var measureDimension = await _db.MeasureDimensions.FindByIdAsync(id, false);
            if (measureDimension == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<MeasureDimension, MeasureDimensionModel>(measureDimension);
            model.IsPrimaryDimension = measureDimension.Id == _measureSettings.BaseDimensionId;

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = measureDimension.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Update)]
        public async Task<IActionResult> EditDimensionPopup(MeasureDimensionModel model, string btnId, string formId)
        {
            var measureDimension = await _db.MeasureDimensions.FindByIdAsync(model.Id);
            if (measureDimension == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await MapperFactory.MapAsync(model, measureDimension);
                    await UpdateDimensionLocalesAsync(measureDimension, model);

                    if (model.IsPrimaryDimension && _measureSettings.BaseDimensionId != measureDimension.Id)
                    {
                        _measureSettings.BaseDimensionId = measureDimension.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                    }

                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Configuration.Entity.Updated"));
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
        public async Task<IActionResult> SetDefaultDimension(int id)
        {
            Guard.NotZero(id, nameof(id));

            _measureSettings.BaseDimensionId = id;
            await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
            await _db.SaveChangesAsync();

            return Json(new { Success = true });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Delete)]
        public async Task<IActionResult> MeasureDimensionDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                try
                {
                    var measureDimensions = await _db.MeasureDimensions.GetManyAsync(ids, true);
                    _db.MeasureDimensions.RemoveRange(measureDimensions);

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

        private async Task UpdateDimensionLocalesAsync(MeasureDimension dimension, MeasureDimensionModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(dimension, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        #endregion
    }
}