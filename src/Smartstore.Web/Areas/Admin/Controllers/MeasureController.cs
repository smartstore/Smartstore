using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Directory;
using Smartstore.ComponentModel;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;

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
        public ActionResult Weights()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> WeightList(GridCommand command)
        {
            var measureWeightModels = await _db.MeasureWeights
                .ApplyGridCommand(command)
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<MeasureWeight, MeasureWeightModel>(x);
                    model.IsPrimaryWeight = x.Id == _measureSettings.BaseWeightId;
                    return model;
                })
                .AsyncToList();

            var measureWeights = await measureWeightModels
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var gridModel = new GridModel<MeasureWeightModel>
            {
                Rows = measureWeights,
                Total = measureWeights.TotalCount
            };

            return Json(gridModel);
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
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId, 0);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.MeasureWeight.Added"));
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
            var measureWeight = _db.MeasureWeights.FindById(id, false);
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
            var measureWeight = _db.MeasureWeights.FindById(model.Id);
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
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseWeightId, 0);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.MeasureWeight.Updated"));
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
                
                if(triedToDeletePrimary && numDeleted == 0)
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
        public ActionResult Dimensions()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Measure.Read)]
        public async Task<IActionResult> DimensionList(GridCommand command)
        {
            var measureDimensionModels = await _db.MeasureDimensions
                .ApplyGridCommand(command)
                .SelectAsync(async x =>
                {
                    var model = await MapperFactory.MapAsync<MeasureDimension, MeasureDimensionModel>(x);
                    model.IsPrimaryDimension = x.Id == _measureSettings.BaseDimensionId;
                    return model;
                })
                .AsyncToList();

            var measureDimensions = await measureDimensionModels
                .ToPagedList(command.Page - 1, command.PageSize)
                .LoadAsync();

            var gridModel = new GridModel<MeasureDimensionModel>
            {
                Rows = measureDimensions,
                Total = measureDimensions.TotalCount
            };

            return Json(gridModel);
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
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId, 0);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.Dimension.Added"));
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
            var measureDimension = _db.MeasureDimensions.FindById(id, false);
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
            var measureDimension = _db.MeasureDimensions.FindById(model.Id);
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
                        await Services.Settings.ApplySettingAsync(_measureSettings, x => x.BaseDimensionId, 0);
                        await _db.SaveChangesAsync();
                    }

                    NotifySuccess(T("Admin.Configuration.MeasureDimension.Updated"));
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
                
                if (triedToDeletePrimary && numDeleted == 0)
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