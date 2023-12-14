using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class LanguageSelectorViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<ILanguageService> _languageService;
        private readonly IUrlService _urlService;
        private readonly LocalizationSettings _localizationSettings;

        public LanguageSelectorViewComponent(
            SmartDbContext db,
            Lazy<ILanguageService> languageService,
            IUrlService urlService,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _languageService = languageService;
            _urlService = urlService;
            _localizationSettings = localizationSettings;
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

                        var model = new LanguageModel
                        {
                            Id = x.Id,

                            // TODO: (core) This is very confusing! ISOCode contains no ISOcode but culture code and vise versa (meaning CultureCode contains ISOcode).
                            // When I tried to change this I saw you used the ISO-code in LocalizedUrlHelper.PrependCultureCode with the terminology culture code.
                            // So I did nothing for now.
                            ISOCode = x.LanguageCulture,
                            CultureCode = x.UniqueSeoCode,
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
            var returnUrls = new Dictionary<string, string>();

            foreach (var lang in availableLanguages)
            {
                var helper = await CreateUrlHelperForLanguageSelectorAsync(lang, workingLanguage.Id);

                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    if (lang.CultureCode == defaultSeoCode && (int)_localizationSettings.DefaultLanguageRedirectBehaviour > 0)
                    {
                        helper.StripCultureCode();
                    }
                    else
                    {
                        helper.PrependCultureCode(lang.CultureCode, true);
                    }
                }

                returnUrls[lang.CultureCode] = helper.Path;
            }

            ViewBag.ReturnUrls = returnUrls;
            ViewBag.UseImages = _localizationSettings.UseImagesForLanguageSelection;
            ViewBag.DisplayLongName = _localizationSettings.DisplayRegionInLanguageSelector;
            ViewBag.AvailableLanguages = availableLanguages;

            return View(templateName);
        }

        private async Task<LocalizedUrlHelper> CreateUrlHelperForLanguageSelectorAsync(LanguageModel model, int currentLanguageId)
        {
            if (currentLanguageId != model.Id)
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
                    var activeSlug = await _urlService.GetActiveSlugAsync(entityId, controllerName, model.Id);
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
