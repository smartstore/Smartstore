using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Smartstore.Admin.Models.Localization;
using Smartstore.ComponentModel;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Threading;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class LanguageController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly IAsyncState _asyncState;

        public LanguageController(
            SmartDbContext db,
            ILanguageService languageService,
            IAsyncState asyncState)
        {
            _db = db;
            _languageService = languageService;
            _asyncState = asyncState;
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

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> List()
        {
            var lastImportInfos = await GetLastResourcesImportInfos();
            var languages = _languageService.GetAllLanguages(true);
            var masterLanguageId = _languageService.GetMasterLanguageId();

            var models = await languages.SelectAsync(async x =>
            {
                var m = await MapperFactory.MapAsync<Language, LanguageModel>(x);
                m.Name = GetCultureDisplayName(x.LanguageCulture) ?? x.Name;

                if (lastImportInfos.TryGetValue(x.Id, out LastResourcesImportInfo info))
                {
                    m.LastResourcesImportOn = info.ImportedOn;
                    m.LastResourcesImportOnString = info.ImportedOn.Humanize(false);
                }

                if (x.Id == masterLanguageId)
                {
                    ViewBag.DefaultLanguageNote = T("Admin.Configuration.Languages.DefaultLanguage.Note", m.Name).Value;
                }

                return m;
            })
            .AsyncToList();

            return View(models);
        }

        [Permission(Permissions.Configuration.Language.Read)]
        public async Task<IActionResult> AvailableLanguages(bool enforce = false)
        {
            var languages = _languageService.GetAllLanguages(true);
            var languageDic = languages.ToDictionarySafe(x => x.LanguageCulture, StringComparer.OrdinalIgnoreCase);

            var downloadState = _asyncState.Get<LanguageDownloadState>();
            var lastImportInfos = await GetLastResourcesImportInfos();
            var checkResult = await CheckAvailableResources(enforce);

            var model = new AvailableLanguageListModel
            {
                Version = checkResult.Version,
                ResourceCount = checkResult.ResourceCount
            };

            foreach (var resources in checkResult.Resources)
            {
                if (resources.Language.Culture.HasValue())
                {
                    languageDic.TryGetValue(resources.Language.Culture, out Language language);

                    var alModel = new AvailableLanguageModel();
                    PrepareAvailableLanguageModel(alModel, resources, lastImportInfos, language, downloadState);

                    model.Languages.Add(alModel);
                }
            }

            return PartialView(model);
        }


        private async Task<CheckAvailableResourcesResult> CheckAvailableResources(bool enforce = false)
        {
            var cacheKey = "admin:language:checkavailableresourcesresult";
            var currentVersion = SmartstoreVersion.CurrentFullVersion;
            CheckAvailableResourcesResult result = null;
            string jsonString = null;

            if (!enforce)
            {
                jsonString = HttpContext.Session.GetString(cacheKey);
            }

            if (jsonString == null)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(10000);
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Smartstore " + currentVersion);
                        client.DefaultRequestHeaders.Add("Authorization-Key", Services.StoreContext.CurrentStore.Url.EmptyNull().TrimEnd('/'));

                        var url = Services.ApplicationContext.AppConfiguration.TranslateCheckUrl.FormatInvariant(currentVersion);
                        var response = await client.GetAsync(url);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            jsonString = await response.Content.ReadAsStringAsync();
                            HttpContext.Session.SetString(cacheKey, jsonString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NotifyError(T("Admin.Configuration.Languages.CheckAvailableLanguagesFailed"));
                    Logger.ErrorsAll(ex);
                }
            }

            if (jsonString.HasValue())
            {
                result = JsonConvert.DeserializeObject<CheckAvailableResourcesResult>(jsonString);
            }

            return result ?? new CheckAvailableResourcesResult();
        }

        private async Task<Dictionary<int, LastResourcesImportInfo>> GetLastResourcesImportInfos()
        {
            Dictionary<int, LastResourcesImportInfo> result = null;

            try
            {
                var attributes = await _db.GenericAttributes
                    .AsNoTracking()
                    .Where(x => x.Key == "LastResourcesImportInfo" && x.KeyGroup == "Language")
                    .ToListAsync();

                result = attributes.ToDictionarySafe(x => x.EntityId, x => JsonConvert.DeserializeObject<LastResourcesImportInfo>(x.Value));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result ?? new Dictionary<int, LastResourcesImportInfo>();
        }

        private void PrepareAvailableLanguageModel(
            AvailableLanguageModel model,
            AvailableResourcesModel resources,
            Dictionary<int, LastResourcesImportInfo> lastImportInfos,
            Language language = null,
            LanguageDownloadState state = null)
        {
            // Source Id (aka SetId), not entity Id!
            model.Id = resources.Id;
            model.PreviousSetId = resources.PreviousSetId;
            model.IsInstalled = language != null;
            model.Name = GetCultureDisplayName(resources.Language.Culture) ?? resources.Language.Name;
            model.LanguageCulture = resources.Language.Culture;
            model.UniqueSeoCode = resources.Language.TwoLetterIsoCode;
            model.Rtl = resources.Language.Rtl;
            model.Version = resources.Version;
            model.Type = resources.Type;
            model.Published = resources.Published;
            model.DisplayOrder = resources.DisplayOrder;
            model.TranslatedCount = resources.TranslatedCount;
            model.TranslatedPercentage = resources.TranslatedPercentage;
            model.IsDownloadRunning = state != null && state.Id == resources.Id;
            model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(resources.UpdatedOn, DateTimeKind.Utc);
            model.UpdatedOnString = model.UpdatedOn.Humanize(false);
            model.FlagImageFileName = GetFlagFileName(resources.Language.Culture);

            if (language != null && lastImportInfos.TryGetValue(language.Id, out LastResourcesImportInfo info))
            {
                // Only show percent at last import if it's less than the current percentage.
                var percentAtLastImport = Math.Round(info.TranslatedPercentage, 2);
                if (percentAtLastImport < model.TranslatedPercentage)
                {
                    model.TranslatedPercentageAtLastImport = percentAtLastImport;
                }

                model.LastResourcesImportOn = info.ImportedOn;
                model.LastResourcesImportOnString = info.ImportedOn.Humanize(false);
            }
        }

        private static string GetCultureDisplayName(string culture)
        {
            if (culture.HasValue())
            {
                try
                {
                    return new CultureInfo(culture).DisplayName;
                }
                catch
                {
                }
            }

            return null;
        }

        private string GetFlagFileName(string culture)
        {
            culture = culture.EmptyNull().ToLower();

            if (culture.HasValue() && culture.SplitToPair(out _, out string cultureRight, "-"))
            {
                var fileName = cultureRight + ".png";

                if (Services.ApplicationContext.WebRoot.FileExists("images/flags/" + fileName))
                {
                    return fileName;
                }
            }

            return null;
        }
    }
}
