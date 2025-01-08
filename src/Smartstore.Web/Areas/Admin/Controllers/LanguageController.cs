using System.Globalization;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Localization;
using Smartstore.ComponentModel;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Engine.Modularity;
using Smartstore.Threading;
using Smartstore.Web.Models.DataGrid;
using Smartstore.Web.Rendering;

namespace Smartstore.Admin.Controllers
{
    public class LanguageController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IModuleCatalog _moduleCatalog;
        private readonly IXmlResourceManager _resourceManager;
        private readonly IAsyncState _asyncState;
        private readonly AsyncRunner _asyncRunner;

        public LanguageController(
            SmartDbContext db,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IStoreMappingService storeMappingService,
            IModuleCatalog moduleCatalog,
            IXmlResourceManager resourceManager,
            IAsyncState asyncState,
            AsyncRunner asyncRunner)
        {
            _db = db;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _storeMappingService = storeMappingService;
            _moduleCatalog = moduleCatalog;
            _resourceManager = resourceManager;
            _asyncState = asyncState;
            _asyncRunner = asyncRunner;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> List()
        {
            var lastImportInfos = await _resourceManager.GetLastResourceSetImportInfosAsync();
            var languages = _languageService.GetAllLanguages(true);
            var masterLanguageId = _languageService.GetMasterLanguageId();
            var mapper = MapperFactory.GetMapper<Language, LanguageModel>();

            var models = await languages.SelectAwait(async x =>
            {
                var m = await mapper.MapAsync(x);
                m.Name = x.GetLocalized(x => x.Name);

                if (lastImportInfos.TryGetValue(x.Id, out var info))
                {
                    m.LastResourcesImportOn = Services.DateTimeHelper.ConvertToUserTime(info.ImportedOn, DateTimeKind.Utc);
                    m.LastResourcesImportOnString = m.LastResourcesImportOn.ToHumanizedString(false);
                }

                if (x.Id == masterLanguageId)
                {
                    ViewBag.DefaultLanguageNote = T("Admin.Configuration.Languages.DefaultLanguage.Note", m.Name);
                }

                return m;
            })
            .AsyncToList();

            return View(models);
        }

        public async Task<IActionResult> LanguageSelected(int customerlanguage)
        {
            var language = await _db.Languages.FindByIdAsync(customerlanguage, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            return Content(T("Admin.Common.DataEditSuccess"));
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> AvailableLanguages(bool enforce = false)
        {
            var model = new AvailableLanguageListModel();
            var languages = _languageService.GetAllLanguages(true);
            var languageDic = languages.ToDictionarySafe(x => x.LanguageCulture, StringComparer.OrdinalIgnoreCase);
            var downloadState = await _asyncState.GetAsync<LanguageDownloadState>();
            var lastImportInfos = await _resourceManager.GetLastResourceSetImportInfosAsync();

            var availableResources = await CheckAvailableResources(enforce);
            if (availableResources != null)
            {
                model.Version = availableResources.Version;
                model.ResourceCount = availableResources.ResourceCount;

                foreach (var resources in availableResources.Resources)
                {
                    if (resources.Language.Culture.HasValue())
                    {
                        languageDic.TryGetValue(resources.Language.Culture, out Language language);

                        var alModel = CreateAvailableLanguageModel(resources, lastImportInfos, language, downloadState);
                        model.Languages.Add(alModel);
                    }
                }
            }
            else
            {
                NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
            }

            return PartialView(model);
        }

        [Permission(Permissions.Configuration.Language.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new LanguageModel();
            AddLocales(model.Locales);
            await PrepareLanguageModel(model, null, false);

            model.DisplayOrder = ((await _db.Languages.MaxAsync(x => (int?)x.DisplayOrder)) ?? 0) + 1;

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Language.Create)]
        public async Task<IActionResult> Create(LanguageModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var language = await MapperFactory.MapAsync<LanguageModel, Language>(model);
                _db.Languages.Add(language);
                await _db.SaveChangesAsync();

                await UpdateLocalesAsync(language, model);
                await _storeMappingService.ApplyStoreMappingsAsync(language, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                var filterLanguages = new List<Language> { language };
                var modules = _moduleCatalog.GetInstalledModules();

                foreach (var module in modules)
                {
                    await _resourceManager.ImportModuleResourcesFromXmlAsync(module, null, false, filterLanguages);
                }

                NotifySuccess(T("Admin.Configuration.Languages.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = language.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareLanguageModel(model, null, true);

            return View(model);
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<Language, LanguageModel>(language);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = language.GetLocalized(x => x.Name, languageId, false, false);
            });

            await PrepareLanguageModel(model, language, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Configuration.Language.Update)]
        public async Task<IActionResult> Edit(LanguageModel model, bool continueEditing)
        {
            var language = await _db.Languages.FindByIdAsync(model.Id);
            if (language == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Ensure we have at least one published language.
                var allLanguages = _languageService.GetAllLanguages();
                if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id && !model.Published)
                {
                    NotifyError(T("Admin.Configuration.Languages.OnePublishedLanguageRequired"));

                    return RedirectToAction(nameof(Edit), new { id = language.Id });
                }

                await MapperFactory.MapAsync(model, language);
                await UpdateLocalesAsync(language, model);
                await _storeMappingService.ApplyStoreMappingsAsync(language, model.SelectedStoreIds);
                await _db.SaveChangesAsync();

                NotifySuccess(T("Admin.Configuration.Languages.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = language.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareLanguageModel(model, language, true);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }

            // Ensure we have at least one published language
            var allLanguages = await _languageService.GetAllLanguagesAsync();
            if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id)
            {
                NotifyError(T("Admin.Configuration.Languages.OnePublishedLanguageRequired"));

                return RedirectToAction(nameof(Edit), new { id = language.Id });
            }

            for (var i = 1; i <= 2; i++)
            {
                try
                {
                    _db.Languages.Remove(language);
                    await _db.SaveChangesAsync();
                    break;
                }
                catch
                {
                    // INFO: SQLite throws "Database disk image is malformed" when the database index IX_LocaleStringResource gets corrupted.
                    // We observed this after Turkish was selected as the default language.
                    if (_db.DataProvider.ProviderType == DbSystemType.SQLite)
                    {
                        await _db.Database.ExecuteSqlAsync($@"REINDEX LocaleStringResource");
                    }
                }
            }

            NotifySuccess(T("Admin.Configuration.Languages.Deleted"));

            return RedirectToAction(nameof(List));
        }

        #region Resources

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> Resources(int languageId)
        {
            var language = await _db.Languages.FindByIdAsync(languageId);
            if (language == null)
            {
                return NotFound();
            }

            ViewBag.AllLanguages = (await _languageService.GetAllLanguagesAsync(true)).ToSelectListItems(languageId);

            var model = new LanguageResourceListModel
            {
                LanguageId = language.Id,
                LanguageName = language.GetLocalized(x => x.Name)
            };

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> LocaleStringResourceList(GridCommand command, LanguageResourceListModel model)
        {
            var language = await _db.Languages.FindByIdAsync(model.LanguageId);
            if (language == null)
            {
                return NotFound();
            }

            var query = _db.LocaleStringResources
                .AsNoTracking()
                .Where(x => x.LanguageId == language.Id);

            if (model.ResourceName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.ResourceName, model.ResourceName);
            }

            if (model.ResourceValue.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.ResourceValue, model.ResourceValue);
            }

            var resources = await query
                .OrderBy(x => x.ResourceName)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            string languageName = language.GetLocalized(x => x.Name);

            var rows = resources
                .AsQueryable()
                .Select(x => new LanguageResourceModel
                {
                    Id = x.Id,
                    LanguageId = language.Id,
                    LanguageName = languageName,
                    ResourceName = x.ResourceName,
                    ResourceValue = x.ResourceValue.EmptyNull(),
                })
                .ToList();

            return Json(new GridModel<LanguageResourceModel>
            {
                Rows = rows,
                Total = resources.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceUpdate(LanguageResourceModel model)
        {
            var success = true;

            model.ResourceName = model.ResourceName.TrimSafe();
            model.ResourceValue = model.ResourceValue.TrimSafe();

            if (ModelState.IsValid)
            {
                var resource = await _db.LocaleStringResources.FindByIdAsync(model.Id);

                // If resourceName changed, ensure it is not being used by another resource.
                if (!resource.ResourceName.EqualsNoCase(model.ResourceName))
                {
                    var resource2 = await _db.LocaleStringResources
                        .AsNoTracking()
                        .Where(x => x.LanguageId == model.LanguageId && x.ResourceName == model.ResourceName)
                        .FirstOrDefaultAsync();

                    if (resource2 != null && resource2.Id != resource.Id)
                    {
                        success = false;
                        NotifyError(T("Admin.Configuration.Languages.Resources.NameAlreadyExists", resource2.ResourceName));
                    }
                }

                if (success)
                {
                    resource.ResourceName = model.ResourceName;
                    resource.ResourceValue = model.ResourceValue;
                    resource.IsTouched = true;

                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                success = false;
                ModelState.Values.SelectMany(x => x.Errors).Each(x => NotifyError(x.ErrorMessage));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceInsert(LanguageResourceModel model, int languageId)
        {
            var success = true;

            model.ResourceName = model.ResourceName.TrimSafe();
            model.ResourceValue = model.ResourceValue.TrimSafe();

            if (ModelState.IsValid)
            {
                if (!await _db.LocaleStringResources.AnyAsync(x => x.LanguageId == languageId && x.ResourceName == model.ResourceName))
                {
                    _db.LocaleStringResources.Add(new LocaleStringResource
                    {
                        LanguageId = languageId,
                        ResourceName = model.ResourceName,
                        ResourceValue = model.ResourceValue,
                        IsTouched = true
                    });

                    await _db.SaveChangesAsync();
                }
                else
                {
                    success = false;
                    NotifyError(T("Admin.Configuration.Languages.Resources.NameAlreadyExists", model.ResourceName));
                }
            }
            else
            {
                success = false;
                ModelState.Values.SelectMany(x => x.Errors).Each(x => NotifyError(x.ErrorMessage));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> LocaleStringResourceDelete(GridSelection selection)
        {
            var success = false;
            var num = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var resources = await _db.LocaleStringResources.GetManyAsync(ids, true);

                _db.LocaleStringResources.RemoveRange(resources);

                num = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = num });
        }

        #endregion

        #region Export / Import / Download

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> ExportXml(int id)
        {
            var language = await _db.Languages.FindByIdAsync(id, false);
            if (language == null)
            {
                return NotFound();
            }

            try
            {
                var xml = await _resourceManager.ExportResourcesToXmlAsync(language);

                return new XmlDownloadResult(xml, $"language-pack-{language.UniqueSeoCode}.xml");
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                return RedirectToAction("List");
            }
        }

        [HttpPost]
        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<ActionResult> ImportXml(int id, ImportModeFlags mode, bool updateTouched, int? availableLanguageSetId)
        {
            var language = await _db.Languages
                .Include(x => x.LocaleStringResources)
                .FindByIdAsync(id);
            if (language == null)
            {
                return NotFound();
            }

            try
            {
                var file = Request.Form.Files["importxmlfile"];

                if (file != null && file.Length > 0)
                {
                    using var stream = file.OpenReadStream();
                    var xml = await stream.AsStringAsync();
                    await _resourceManager.ImportResourcesFromXmlAsync(language, xml, null, false, mode, updateTouched);

                    NotifySuccess(T("Admin.Configuration.Languages.Imported"));
                }
                else if (availableLanguageSetId > 0)
                {
                    var availableResources = await CheckAvailableResources();
                    if (availableResources != null)
                    {
                        if (await _resourceManager.DownloadResourceSetAsync(availableLanguageSetId.Value, availableResources))
                        {
                            NotifySuccess(T("Admin.Configuration.Languages.Imported"));
                        }
                    }
                    else
                    {
                        NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
                    }
                }
                else
                {
                    NotifyError(T("Admin.Configuration.Languages.UploadFileOrSelectLanguage"));
                }
            }
            catch (Exception ex)
            {
                NotifyError(ex);
                Logger.ErrorsAll(ex);
            }

            return RedirectToAction(nameof(Edit), new { id = language.Id });
        }

        [Permission(Permissions.Configuration.Language.EditResource)]
        public async Task<IActionResult> Download(int setId)
        {
            var context = new DownloadContext
            {
                StringResourcesSetId = setId,
                AvailableResources = await CheckAvailableResources()
            };

            if (context.AvailableResources != null)
            {
                _ = _asyncRunner.RunTask((scope, ct, state) =>
                {
                    var ctx = (DownloadContext)state;
                    return scope.Resolve<IXmlResourceManager>().DownloadResourceSetAsync(ctx.StringResourcesSetId, ctx.AvailableResources, ct);
                }, context);
            }
            else
            {
                NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        public async Task<IActionResult> DownloadProgress()
        {
            var state = await _asyncState.GetAsync<LanguageDownloadState>();
            if (state != null)
            {
                var message = string.Empty;
                switch (state.Step)
                {
                    case LanguageDownloadStep.DownloadResources:
                        message = T("Admin.Configuration.Languages.DownloadingResources");
                        break;
                    case LanguageDownloadStep.ImportResources:
                        message = T("Admin.Configuration.Languages.ImportResources");
                        break;
                }

                var info = new
                {
                    id = state.Id,
                    percent = state.ProgressPercent,
                    message
                };

                return Json(new object[] { info });
            }

            return Json(new EmptyResult());
        }

        #endregion

        private async Task<ResourceSetsResponse> CheckAvailableResources(bool enforce = false)
        {
            const string cacheKey = "admin:language:checkavailableresourcesresult";
            ResourceSetsResponse result = null;
            string jsonString = null;

            if (!enforce)
            {
                jsonString = HttpContext.Session.GetString(cacheKey);
            }

            if (jsonString == null)
            {
                try
                {
                    result = await _resourceManager.GetOnlineResourceSetsAsync();
                    if (result != null)
                    {
                        jsonString = JsonConvert.SerializeObject(result);
                        HttpContext.Session.SetString(cacheKey, jsonString);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorsAll(ex);
                }
            }

            if (result == null && jsonString.HasValue())
            {
                result = JsonConvert.DeserializeObject<ResourceSetsResponse>(jsonString);
            }

            result?.Resources
                .Where(x => x.Language != null)
                .Each(x => x.Language.Culture = CultureHelper.GetValidCultureCode(x.Language.Culture));

            return result;
        }

        private async Task PrepareLanguageModel(LanguageModel model, Language language, bool excludeProperties)
        {
            var twoLetterLanguageCodes = new List<SelectListItem>();
            var countryFlags = new List<SelectListItem>();
            var lastImportInfos = await _resourceManager.GetLastResourceSetImportInfosAsync();

            var allCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .OrderBy(x => x.DisplayName)
                .ToList();

            ViewBag.Cultures = allCultures.ToSelectListItems();

            // Get two-letter language codes.
            foreach (var culture in allCultures)
            {
                if (!twoLetterLanguageCodes.Any(x => x.Value.EqualsNoCase(culture.TwoLetterISOLanguageName)))
                {
                    // Display language name is not provided by net framework.
                    var index = culture.DisplayName.EmptyNull().IndexOf(" (");

                    if (index == -1)
                    {
                        index = culture.DisplayName.EmptyNull().IndexOf(" [");
                    }

                    var displayName = "{0} [{1}]".FormatInvariant(
                        index == -1 ? culture.DisplayName : culture.DisplayName[..index],
                        culture.TwoLetterISOLanguageName);

                    if (culture.TwoLetterISOLanguageName.Length == 2)
                    {
                        twoLetterLanguageCodes.Add(new() { Text = displayName, Value = culture.TwoLetterISOLanguageName });
                    }
                }
            }

            ViewBag.TwoLetterLanguageCodes = twoLetterLanguageCodes;

            // Get country flags.
            var allCountries = await _db.Countries
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var allCountryNames = allCountries.ToDictionarySafe(x =>
                x.TwoLetterIsoCode.EmptyNull().ToLower(),
                x => x.GetLocalized(y => y.Name, Services.WorkContext.WorkingLanguage, true, false));

            var allCulturesMap = allCultures.ToDictionarySafe(x => x.TwoLetterISOLanguageName);
            var flagsDir = await Services.ApplicationContext.WebRoot.GetDirectoryAsync("images/flags");
            var flags = flagsDir.EnumerateFilesAsync();

            await foreach (var flag in flags)
            {
                var name = flag.NameWithoutExtension.EmptyNull().ToLower();
                string countryDescription = null;

                if (allCountryNames.TryGetValue(name, out var countryName))
                {
                    countryDescription = $"{countryName} [{name}]";
                }

                if (countryDescription.IsEmpty() && allCulturesMap.TryGetValue(name, out var ci))
                {
                    countryDescription = $"{ci.DisplayName} [{name}]";
                }

                countryFlags.Add(new() { Text = countryDescription.NullEmpty() ?? name, Value = flag.Name });
            }

            ViewBag.CountryFlags = countryFlags.OrderBy(x => x.Text).ToList();

            if (language != null)
            {
                if (!excludeProperties)
                {
                    model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(language);
                }

                if (lastImportInfos.TryGetValue(language.Id, out var info))
                {
                    model.LastResourcesImportOn = Services.DateTimeHelper.ConvertToUserTime(info.ImportedOn, DateTimeKind.Utc);
                    model.LastResourcesImportOnString = model.LastResourcesImportOn.ToHumanizedString(false);
                }

                string cultureParentName = null;
                try
                {
                    var ci = CultureInfo.GetCultureInfo(language.LanguageCulture);
                    if (!ci.IsNeutralCulture && ci.Parent != null)
                    {
                        cultureParentName = ci.Parent.Name;
                    }
                }
                catch
                {
                }

                // Provide downloadable resources.
                var availableResources = await CheckAvailableResources();
                if (availableResources != null)
                {
                    ViewBag.DownloadableLanguages = availableResources.Resources
                        .Where(x => x.Published)
                        .Select(x =>
                        {
                            var srcCulture = x.Language.Culture;
                            if (srcCulture.HasValue())
                            {
                                var displayOrder = srcCulture.EqualsNoCase(language.LanguageCulture) ? 1 : 0;
                                if (displayOrder == 0 && cultureParentName.EqualsNoCase(srcCulture))
                                    displayOrder = 2;
                                if (displayOrder == 0 && x.Language.TwoLetterIsoCode.EqualsNoCase(language.UniqueSeoCode))
                                    displayOrder = 3;

                                if (displayOrder != 0)
                                {
                                    var alModel = CreateAvailableLanguageModel(x, lastImportInfos, language);
                                    alModel.DisplayOrder = displayOrder;
                                    return alModel;
                                }
                            }

                            return null;
                        })
                        .Where(x => x != null)
                        .ToList();
                }
                else
                {
                    ViewBag.DownloadableLanguages = new List<AvailableLanguageModel>();
                    NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
                }
            }
        }

        private AvailableLanguageModel CreateAvailableLanguageModel(
            ResourceSet resources,
            Dictionary<int, ResourceSetImportInfo> lastImportInfos,
            Language language = null,
            LanguageDownloadState state = null)
        {
            var updatedOn = Services.DateTimeHelper.ConvertToUserTime(resources.UpdatedOn, DateTimeKind.Utc);

            var model = new AvailableLanguageModel
            {
                // Source Id (aka SetId), not entity Id!
                Id = resources.Id,
                PreviousSetId = resources.PreviousSetId,
                IsInstalled = language != null,
                Name = CultureHelper.GetLanguageDisplayName(resources.Language.Culture) ?? resources.Language.Name,
                LanguageCulture = resources.Language.Culture,
                UniqueSeoCode = resources.Language.TwoLetterIsoCode,
                Rtl = resources.Language.Rtl,
                Version = resources.Version,
                Type = resources.Type,
                Published = resources.Published,
                DisplayOrder = resources.DisplayOrder,
                TranslatedCount = resources.TranslatedCount,
                TranslatedPercentage = resources.TranslatedPercentage,
                IsDownloadRunning = state != null && state.Id == resources.Id,
                UpdatedOn = updatedOn,
                UpdatedOnString = updatedOn.ToHumanizedString(false)
            };

            var parts = resources.Language.Culture.SplitSafe('-').ToArray();
            if (parts.Length > 0)
            {
                var fileName = parts[^1].EmptyNull().ToLowerInvariant() + ".png";
                if (Services.ApplicationContext.WebRoot.FileExists("images/flags/" + fileName))
                {
                    model.FlagImageFileName = fileName;
                }
            }

            if (language != null && lastImportInfos.TryGetValue(language.Id, out ResourceSetImportInfo info))
            {
                // Only show percent at last import if it's less than the current percentage.
                var percentAtLastImport = Math.Round(info.TranslatedPercentage, 2);
                if (percentAtLastImport < model.TranslatedPercentage)
                {
                    model.TranslatedPercentageAtLastImport = percentAtLastImport;
                }

                model.LastResourcesImportOn = Services.DateTimeHelper.ConvertToUserTime(info.ImportedOn, DateTimeKind.Utc);
                model.LastResourcesImportOnString = model.LastResourcesImportOn.ToHumanizedString(false);
            }

            return model;
        }

        private async Task UpdateLocalesAsync(Language language, LanguageModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(language, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        class DownloadContext
        {
            public int StringResourcesSetId { get; set; }
            public ResourceSetsResponse AvailableResources { get; set; }
        }
    }
}
