using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
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
        public async Task<IActionResult> WeightList(GridCommand command)
        {
            var measureWeights = await _db.MeasureWeights
                .AsNoTracking()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var measureWeightModels = await measureWeights
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<MeasureWeight, MeasureWeightModel>(x);
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
        public async Task<IActionResult> WeightUpdate(MeasureWeightModel model)
        {
            var success = false;
            var measureWeight = await _db.MeasureWeights.FindByIdAsync(model.Id);

            if (measureWeight != null)
            {
                if (model.IsPrimaryWeight && _measureSettings.BaseWeightId != measureWeight.Id)
                {
                    _measureSettings.BaseWeightId = measureWeight.Id;
                    await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                    await _db.SaveChangesAsync();
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
                    await _db.SaveChangesAsync();

                    if (model.IsPrimaryWeight)
                    {
                        _measureSettings.BaseWeightId = measureWeight.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.Entity.Added"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
        [ValidateAntiForgeryToken]
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
                    await _db.SaveChangesAsync();

                    if (model.IsPrimaryWeight && _measureSettings.BaseWeightId != measureWeight.Id)
                    {
                        _measureSettings.BaseWeightId = measureWeight.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.Entity.Updated"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
        public async Task<IActionResult> DeleteMeasureWeights(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var measureWeights = await _db.MeasureWeights.GetManyAsync(ids, true);
                var triedToDeletePrimary = false;

                foreach (var weight in measureWeights)
                {
                    if (weight.Id == _measureSettings.BaseWeightId)
                    {
                        triedToDeletePrimary = true;
                        NotifyError(T("Admin.Configuration.Measures.Dimensions.CantDeletePrimary"));
                    }
                    else
                    {
                        _db.MeasureWeights.Remove(weight);
                    }
                }

                numDeleted = await _db.SaveChangesAsync();

                success = triedToDeletePrimary && numDeleted == 0 ? false : true;
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
        public async Task<IActionResult> DimensionList(GridCommand command)
        {
            var measureDimensions = await _db.MeasureDimensions
                .AsNoTracking()
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var measureDimensionModels = await measureDimensions.SelectAsync(async x =>
            {
                var model = await MapperFactory.MapAsync<MeasureDimension, MeasureDimensionModel>(x);
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
        public async Task<IActionResult> DimensionUpdate(MeasureDimensionModel model)
        {
            var success = false;
            var measureDimension = await _db.MeasureDimensions.FindByIdAsync(model.Id);

            if (measureDimension != null)
            {
                if (model.IsPrimaryDimension && _measureSettings.BaseDimensionId != measureDimension.Id)
                {
                    _measureSettings.BaseDimensionId = measureDimension.Id;
                    await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                    await _db.SaveChangesAsync();
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
                    await _db.SaveChangesAsync();

                    if (model.IsPrimaryDimension)
                    {
                        _measureSettings.BaseDimensionId = measureDimension.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.Entity.Added"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
        [ValidateAntiForgeryToken]
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
                    await _db.SaveChangesAsync();

                    if (model.IsPrimaryDimension && _measureSettings.BaseDimensionId != measureDimension.Id)
                    {
                        _measureSettings.BaseDimensionId = measureDimension.Id;
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.Entity.Updated"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
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
        public async Task<IActionResult> DeleteMeasureDimensions(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var measureDimension = await _db.MeasureDimensions.GetManyAsync(ids, true);
                var triedToDeletePrimary = false;

                foreach (var dimension in measureDimension)
                {
                    if (dimension.Id == _measureSettings.BaseDimensionId)
                    {
                        triedToDeletePrimary = true;
                        NotifyError(T("Admin.Configuration.Measures.Dimensions.CantDeletePrimary"));
                    }
                    else
                    {
                        _db.MeasureDimensions.Remove(dimension);
                    }
                }

                numDeleted = await _db.SaveChangesAsync();

                success = triedToDeletePrimary && numDeleted == 0 ? false : true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [NonAction]
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