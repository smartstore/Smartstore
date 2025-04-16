using Microsoft.AspNetCore.Html;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Http;
using Smartstore.Web.Infrastructure.Hooks;

namespace Smartstore.Web.Components
{
    public class LocalizedUrl
    {
        public ExtendedLanguageInfo Language { get; init; }
        public string Url { get; init; }
    }

    public class LanguageSelectorViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<ILanguageService> _languageService;
        private readonly IUrlService _urlService;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly LocalizationSettings _localizationSettings;
        private readonly SeoSettings _seoSettings;

        public LanguageSelectorViewComponent(
            SmartDbContext db,
            Lazy<ILanguageService> languageService,
            IUrlService urlService,
            Lazy<IWidgetProvider> widgetProvider,
            LocalizationSettings localizationSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _languageService = languageService;
            _urlService = urlService;
            _widgetProvider = widgetProvider;
            _localizationSettings = localizationSettings;
            _seoSettings = seoSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(string templateName = "Default")
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            var workingLanguage = Services.WorkContext.WorkingLanguage;
            var key = ModelCacheInvalidator.AVAILABLE_LANGUAGES_MODEL_KEY.FormatInvariant(workingLanguage.Id, storeId, _localizationSettings.UseNativeNameInLanguageSelector);

            var availableLanguages = await Services.Cache.GetAsync(key, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));

                var languages = await _db.Languages
                    .AsNoTracking()
                    .ApplyStandardFilter(false, storeId)
                    .ToListAsync();

                var masterLanguageId = await _languageService.Value.GetMasterLanguageIdAsync(storeId);

                var result = languages
                    .Select(x =>
                    {
                        CultureHelper.TryGetCultureInfoForLocale(x.LanguageCulture, out var culture);
                        CultureHelper.TryGetCultureInfoForLocale(x.GetTwoLetterISOLanguageName(), out var neutralCulture);

                        neutralCulture ??= culture?.Parent ?? culture;

                        var localizedName = x.GetLocalized(x => x.Name, workingLanguage, returnDefaultValue: workingLanguage.Id == masterLanguageId).Value.NullEmpty();
                        var defaultLocalizedName = x.GetLocalized(x => x.Name, workingLanguage, returnDefaultValue: true).Value;
                        string name;
                        string shortName;

                        if (_localizationSettings.UseNativeNameInLanguageSelector)
                        {
                            name = culture?.NativeName ?? localizedName;
                            shortName = neutralCulture?.NativeName ?? localizedName;
                        }
                        else
                        {
                            name = localizedName ?? culture?.NativeName;
                            shortName = localizedName ?? neutralCulture?.NativeName;
                        }

                        var model = new ExtendedLanguageInfo
                        {
                            Id = x.Id,
                            LanguageCulture = x.LanguageCulture,
                            UniqueSeoCode = x.UniqueSeoCode,
                            FlagImageFileName = x.FlagImageFileName,
                            Name = CultureHelper.NormalizeLanguageDisplayName(name ?? defaultLocalizedName, stripRegion: false, culture: culture),
                            ShortName = CultureHelper.NormalizeLanguageDisplayName(shortName ?? defaultLocalizedName, stripRegion: true, culture: culture),
                            LocalizedName = CultureHelper.NormalizeLanguageDisplayName(defaultLocalizedName, stripRegion: false, culture: culture),
                            LocalizedShortName = CultureHelper.NormalizeLanguageDisplayName(defaultLocalizedName, stripRegion: true, culture: culture)
                        };

                        return model;
                    })
                    .ToList();

                return result;
            });

            if (availableLanguages.Count < 2)
            {
                return Empty();
            }

            var defaultSeoCode = await _languageService.Value.GetMasterLanguageSeoCodeAsync();
            var localizedUrls = new List<LocalizedUrl>(availableLanguages.Count);
            var alternateLinks = new HtmlContentBuilder();

            foreach (var lang in availableLanguages)
            {
                var helper = await CreateUrlHelperForLanguageSelectorAsync(lang, workingLanguage.Id);

                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    if (lang.UniqueSeoCode == defaultSeoCode && (int)_localizationSettings.DefaultLanguageRedirectBehaviour > 0)
                    {
                        helper.StripCultureCode();
                    }
                    else
                    {
                        helper.PrependCultureCode(lang.UniqueSeoCode, true);
                    }
                }

                localizedUrls.Add(new()
                {
                    Language = lang,
                    Url = helper.Path
                });

                if (_seoSettings.AddAlternateHtmlLinks)
                {
                    var url = WebHelper.GetAbsoluteUrl(helper.Path, Request);
                    alternateLinks.AppendHtmlLine($"<link rel=\"alternate\" hreflang=\"{lang.UniqueSeoCode}\" href=\"{url}\" />");
                }
            }

            if (alternateLinks.Count > 0)
            {
                _widgetProvider.Value.RegisterHtml("head_links", alternateLinks);
            }

            ViewBag.LocalizedUrls = localizedUrls;
            ViewBag.UseImages = _localizationSettings.UseImagesForLanguageSelection;
            ViewBag.DisplayLongName = _localizationSettings.DisplayRegionInLanguageSelector;

            return View(templateName);
        }

        private async Task<LocalizedUrlHelper> CreateUrlHelperForLanguageSelectorAsync(ExtendedLanguageInfo info, int currentLanguageId)
        {
            if (currentLanguageId != info.Id)
            {
                var routeValues = Request.RouteValues;
                var controllerName = routeValues.GetControllerName();

                if (!routeValues.TryGetValue(controllerName + "id", out var val))
                {
                    controllerName = routeValues.GetActionName();
                    routeValues.TryGetValue(controllerName + "id", out val);
                }

                int entityId = 0;
                if (val != null)
                {
                    entityId = val.Convert<int>();
                }

                if (entityId > 0)
                {
                    var activeSlug = await _urlService.GetActiveSlugAsync(entityId, controllerName, info.Id);
                    if (activeSlug.IsEmpty())
                    {
                        // Fallback to default value.
                        activeSlug = await _urlService.GetActiveSlugAsync(entityId, controllerName, 0);
                    }

                    if (activeSlug.HasValue())
                    {
                        return new LocalizedUrlHelper(Request.PathBase, activeSlug + Request.QueryString);
                    }
                }
            }

            return new LocalizedUrlHelper(Request.PathBase, Request.Path + Request.QueryString);
        }
    }
}
