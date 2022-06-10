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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var key = ModelCacheInvalidator.AVAILABLE_LANGUAGES_MODEL_KEY.FormatInvariant(Services.StoreContext.CurrentStore.Id);
            var availableLanguages = await Services.Cache.GetAsync(key, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));

                var languages = await _db.Languages
                    .AsNoTracking()
                    .ApplyStandardFilter(false, Services.StoreContext.CurrentStore.Id)
                    .ToListAsync();

                var result = languages
                    .Select(x => new LanguageModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        NativeName = CultureHelper.GetLanguageNativeName(x.LanguageCulture) ?? x.Name,
                        ISOCode = x.LanguageCulture,
                        CultureCode = x.UniqueSeoCode,
                        FlagImageFileName = x.FlagImageFileName
                    })
                    .ToList();

                return result;
            });

            if (availableLanguages.Count < 2)
            {
                return Empty();
            }

            ViewBag.AvailableLanguages = availableLanguages;

            var workingLanguage = Services.WorkContext.WorkingLanguage;
            string defaultSeoCode = await _languageService.Value.GetMasterLanguageSeoCodeAsync();
            var returnUrls = new Dictionary<string, string>();

            foreach (var lang in availableLanguages)
            {
                var helper = await CreateUrlHelperForLanguageSelectorAsync(lang, workingLanguage.Id);

                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    if (lang.CultureCode == defaultSeoCode && (int)(_localizationSettings.DefaultLanguageRedirectBehaviour) > 0)
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

            return View();
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
                        var helper = new LocalizedUrlHelper(Request.PathBase, activeSlug);
                        return helper;
                    }
                }
            }

            return new LocalizedUrlHelper(Request);
        }
    }
}
