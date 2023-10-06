using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.UrlRecord;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

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

        [Permission(Permissions.System.UrlRecord.Read)]
        public IActionResult Index(string entityName, int? entityId)
        {
            TempData["entityName"] = entityName;
            TempData["entityId"] = entityId;

            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.UrlRecord.Read)]
        public async Task<IActionResult> List()
        {
            TempData.TryGetValueAs<string>("entityName", out var entityName);
            TempData.TryGetAndConvertValue<int?>("entityId", out var entityId);

            var model = new UrlRecordListModel
            {
                EntityName = entityName,
                EntityId = entityId
            };

            await PrepareAvailableLanguages();

            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var entityNames = await _db.UrlRecords
                .Select(x => x.EntityName)
                .Distinct()
                .ToListAsync();

            var resourceNamesMap = entityNames.ToDictionarySafe(x => x, x => "Common.Entity." + x);
            var allResourceNames = resourceNamesMap.Values.ToArray();

            var localizedNames = (await _db.LocaleStringResources
                .Where(x => allResourceNames.Contains(x.ResourceName) && x.LanguageId == languageId)
                .Select(x => new { x.ResourceName, x.ResourceValue })
                .ToListAsync())
                .ToDictionarySafe(x => x.ResourceName, x => x.ResourceValue);

            ViewBag.EntityNames = entityNames
                .Select(x => new SelectListItem
                {
                    Text = localizedNames.Get(resourceNamesMap.Get(x).EmptyNull()).NullEmpty() ?? x,
                    Value = x,
                    Selected = x.EqualsNoCase(entityName)
                })
                .ToList()
                .OrderBy(x => x.Text)
                .ToList();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.UrlRecord.Read)]
        public async Task<IActionResult> UrlRecordList(GridCommand command, UrlRecordListModel model)
        {
            var urlRecords = await _db.UrlRecords
                .AsNoTracking()
                .ApplySlugFilter(model.SeName, false)
                .ApplyEntityFilter(model.EntityName, model.EntityId ?? 0, model.LanguageId, model.IsActive)
                .OrderBy(x => x.Slug)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var slugsPerEntity = await _urlService.CountSlugsPerEntityAsync(urlRecords.ToDistinctArray(x => x.Id));

            var models = urlRecords
                .Select(x =>
                {
                    var model = new UrlRecordModel();
                    PrepareUrlRecordModel(model, x);

                    model.SlugsPerEntity = slugsPerEntity.ContainsKey(x.Id) ? slugsPerEntity[x.Id] : 0;
                    model.EditUrl = Url.Action(nameof(Edit), "UrlRecord", new { id = x.Id });

                    return model;
                })
                .ToList();

            return Json(new GridModel<UrlRecordModel>
            {
                Rows = models,
                Total = await urlRecords.GetTotalCountAsync()
            });
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
            await PrepareAvailableLanguages();

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
                    ModelState.AddModelError(nameof(UrlRecordModel.IsActive), T("Admin.System.SeNames.ActiveSlugAlreadyExist"));
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

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = urlRecord.Id })
                    : RedirectToAction(nameof(List));
            }

            PrepareUrlRecordModel(model, null);
            await PrepareAvailableLanguages();

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

        private async Task PrepareAvailableLanguages()
        {
            ViewBag.AvailableLanguages = (await _languageService.GetAllLanguagesAsync(true)).ToSelectListItems();
            ViewBag.AvailableLanguages.Insert(0, new SelectListItem { Text = T("Admin.System.SeNames.Language.Standard"), Value = "0" });
        }

        private void PrepareUrlRecordModel(
            UrlRecordModel model,
            UrlRecord urlRecord,
            Dictionary<int, Language> allLanguages = null)
        {
            if (urlRecord != null)
            {
                allLanguages ??= _languageService.GetAllLanguages(true).ToDictionary(x => x.Id);

                var localizedEntityName = Services.Localization.GetResource("Common.Entity." + urlRecord.EntityName, 0, false, string.Empty, true);

                model.Id = urlRecord.Id;
                model.Slug = urlRecord.Slug;
                model.EntityName = urlRecord.EntityName;
                model.LocalizedEntityName = localizedEntityName.NullEmpty() ?? urlRecord.EntityName;
                model.EntityId = urlRecord.EntityId;
                model.IsActive = urlRecord.IsActive;
                model.LanguageId = urlRecord.LanguageId;

                var routeValues = SlugRouteTransformer.GetRouteValuesFor(urlRecord, RouteTarget.Edit);
                model.EntityUrl = routeValues != null
                    ? Url.RouteUrl(routeValues)
                    : Url.Action("Edit", urlRecord.EntityName, new { id = urlRecord.EntityId });

                if (urlRecord.LanguageId == 0)
                {
                    model.Language = T("Admin.System.SeNames.Language.Standard");
                }
                else if (allLanguages.TryGetValue(urlRecord.LanguageId, out var language))
                {
                    model.Language = language?.GetLocalized(x => x.Name) ?? StringExtensions.NotAvailable;
                    model.FlagImageUrl = Url.Content("~/images/flags/" + language.FlagImageFileName);
                }
                else
                {
                    model.Language = StringExtensions.NotAvailable;
                }
            }
        }
    }
}
