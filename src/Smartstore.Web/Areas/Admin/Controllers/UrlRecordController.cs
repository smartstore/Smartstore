using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.UrlRecord;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class UrlRecordController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly IUrlService _urlService;

        public UrlRecordController(SmartDbContext db, ILanguageService languageService, IUrlService urlService)
        {
            _db = db;
            _languageService = languageService;
            _urlService = urlService;
        }

        private void PrepareAvailableLanguages()
        {
            var allLanguages = _languageService.GetAllLanguages(true);

            ViewBag.AvailableLanguages = _languageService
                .GetAllLanguages()
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();

            ViewBag.AvailableLanguages.Insert(0, new SelectListItem { Text = T("Admin.System.SeNames.Language.Standard"), Value = "0" });
        }

        private void PrepareUrlRecordModel(UrlRecordModel model, UrlRecord urlRecord, bool forList = false)
        {
            if (!forList)
            {
                PrepareAvailableLanguages();
            }

            if (urlRecord != null)
            {
                model.Id = urlRecord.Id;
                model.Slug = urlRecord.Slug;
                model.EntityName = urlRecord.EntityName;
                model.EntityId = urlRecord.EntityId;
                model.IsActive = urlRecord.IsActive;
                model.LanguageId = urlRecord.LanguageId;

                var routeValues = SlugRouteTransformer.Routers
                    .Select(x => x.GetRouteValues(urlRecord, null, RouteTarget.Edit))
                    .Where(x => x != null)
                    .FirstOrDefault();

                if (routeValues != null)
                {
                    model.EntityUrl = Url.RouteUrl(routeValues);
                }
                else
                {
                    model.EntityUrl = Url.Action("Edit", urlRecord.EntityName, new { id = urlRecord.EntityId });
                }
            }
        }

        [Permission(Permissions.System.UrlRecord.Read)]
        public IActionResult List(string entityName, int? entityId)
        {
            var model = new UrlRecordListModel
            {
                EntityName = entityName,
                EntityId = entityId
            };

            PrepareAvailableLanguages();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.UrlRecord.Read)]
        public async Task<IActionResult> UrlRecordList(GridCommand command, UrlRecordListModel model)
        {
            var allLanguages = _languageService.GetAllLanguages(true);
            var defaultLanguageName = T("Admin.System.SeNames.Language.Standard");

            var urlRecords = await _db.UrlRecords
                .AsNoTracking()
                .ApplySlugFilter(model.SeName, false)
                .ApplyEntityFilter(model.EntityName, model.EntityId ?? 0, model.LanguageId ?? 0, model.IsActive)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var slugsPerEntity = await _urlService.CountSlugsPerEntityAsync(urlRecords.Select(x => x.Id).Distinct().ToArray());

            var urlRecordModels = urlRecords
                .Select(x =>
                {
                    string languageName;

                    if (x.LanguageId == 0)
                    {
                        languageName = defaultLanguageName;
                    }
                    else
                    {
                        var language = allLanguages.FirstOrDefault(y => y.Id == x.LanguageId);
                        languageName = language != null ? language.Name : string.Empty.NaIfEmpty();
                    }

                    var urlRecordModel = new UrlRecordModel();
                    PrepareUrlRecordModel(urlRecordModel, x, true);

                    urlRecordModel.Language = languageName;
                    urlRecordModel.SlugsPerEntity = slugsPerEntity.ContainsKey(x.Id) ? slugsPerEntity[x.Id] : 0;
                    urlRecordModel.EditUrl = Url.Action("Edit", "UrlRecord", new { id = x.Id });
                    urlRecordModel.FilterUrl = Url.Action("List", "UrlRecord", new { entityName = x.EntityName, entityId = x.EntityId });
                    return urlRecordModel;
                })
                .ToList();

            var gridModel = new GridModel<UrlRecordModel>
            {
                Rows = urlRecordModels,
                Total = await urlRecords.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [Permission(Permissions.System.UrlRecord.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var urlRecord = await _db.UrlRecords.FindByIdAsync(id, false);
            if (urlRecord == null)
            {
                return NotFound();
            }

            var model = new UrlRecordModel();
            PrepareUrlRecordModel(model, urlRecord);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.System.UrlRecord.Update)]
        public async Task<IActionResult> Edit(UrlRecordModel model, bool continueEditing)
        {
            var urlRecord = await _db.UrlRecords.FindByIdAsync(model.Id);
            if (urlRecord == null)
            {
                return NotFound();
            }

            if (!urlRecord.IsActive && model.IsActive)
            {
                var urlRecords = (await _urlService.GetUrlRecordCollectionAsync(model.EntityName, new[] { model.LanguageId }, new[] { model.EntityId }))
                    .Where(x => x.IsActive == true)
                    .ToList();

                if (urlRecords.Count > 0)
                {
                    ModelState.AddModelError("IsActive", T("Admin.System.SeNames.ActiveSlugAlreadyExist"));
                }
            }

            if (ModelState.IsValid)
            {
                urlRecord.Slug = model.Slug;
                urlRecord.EntityName = model.EntityName;
                urlRecord.IsActive = model.IsActive;
                urlRecord.LanguageId = model.LanguageId;

                await _db.SaveChangesAsync();
                
                NotifySuccess(T("Admin.Common.DataEditSuccess"));

                return continueEditing ? RedirectToAction(nameof(Edit), new { id = urlRecord.Id }) : RedirectToAction(nameof(List));
            }

            PrepareUrlRecordModel(model, null);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.UrlRecord.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var urlRecord = await _db.UrlRecords.FindByIdAsync(id);
            if (urlRecord == null)
            {
                return NotFound();
            }

            try
            {
                _db.UrlRecords.Remove(urlRecord);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Common.TaskSuccessfullyProcessed"));

                return RedirectToAction(nameof(List));
            }
            catch (Exception ex)
            {
                NotifyError(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = urlRecord.Id });
        }

        [HttpPost]
        [Permission(Permissions.System.UrlRecord.Delete)]
        public async Task<IActionResult> UrlRecordDelete(GridSelection selection)
        {
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var toDelete = await _db.UrlRecords
                    .Where(x => ids.Contains(x.Id))
                    .ToListAsync();

                numDeleted = toDelete.Count;

                _db.UrlRecords.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }
    }
}
