using System.Globalization;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Localization
{
    public partial class LocalizedEntityHelper
    {
        private readonly SmartDbContext _db;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ILocalizationService _localizationService;
        private readonly IUrlService _urlService;
        private readonly ISettingFactory _settingFactory;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly SeoSettings _seoSettings;

        private int? _languageCount;
        private Language _masterLanguage;

        public LocalizedEntityHelper(
            SmartDbContext db,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService,
            IUrlService urlService,
            ISettingFactory settingFactory,
            IWorkContext workContext,
            IStoreContext storeContext,
            SeoSettings seoSettings)
        {
            _db = db;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _localizationService = localizationService;
            _urlService = urlService;
            _settingFactory = settingFactory;
            _workContext = workContext;
            _storeContext = storeContext;
            _seoSettings = seoSettings;
        }

        private int LanguageCount
        {
            get => _languageCount ??= _db.Languages.ApplyStandardFilter().Count();
        }

        private Language MasterLanguage
        {
            get => _masterLanguage ??= _db.Languages.FindById(_languageService.GetMasterLanguageId());
        }

        public LocalizedValue<TProp> GetLocalizedValue<T, TProp>(T obj,
            int id, // T is BaseEntity = EntityId, T is ISetting = StoreId
            string localeKeyGroup,
            string localeKey,
            Func<T, TProp> fallback,
            object requestLanguageIdOrObj, // Id or Language
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true,
            bool detectEmptyHtml = false)
            where T : class
        {
            Guard.NotNull(obj, nameof(obj));

            TProp result = default;
            var str = string.Empty;

            Language currentLanguage = null;
            Language requestLanguage = null;

            if (requestLanguageIdOrObj is not Language language)
            {
                if (requestLanguageIdOrObj is int requestLanguageId)
                {
                    requestLanguage = _db.Languages.FindById(requestLanguageId);
                }
            }
            else
            {
                requestLanguage = language;
            }

            if (requestLanguage == null)
            {
                requestLanguage = _workContext.WorkingLanguage;
            }

            // Ensure that we have at least two published languages
            var loadLocalizedValue = true;
            if (ensureTwoPublishedLanguages)
            {
                loadLocalizedValue = LanguageCount > 1;
            }

            // Localized value
            if (loadLocalizedValue)
            {
                str = _localizedEntityService.GetLocalizedValue(requestLanguage.Id, id, localeKeyGroup, localeKey);

                if (detectEmptyHtml && HtmlUtility.IsEmptyHtml(str))
                {
                    str = string.Empty;
                }

                if (!string.IsNullOrEmpty(str))
                {
                    currentLanguage = requestLanguage;
                    result = str.Convert<TProp>(CultureInfo.InvariantCulture);
                }
            }

            // Set default value if required
            if (returnDefaultValue && string.IsNullOrEmpty(str))
            {
                currentLanguage = MasterLanguage;
                result = fallback(obj);
            }

            if (currentLanguage == null)
            {
                currentLanguage = requestLanguage;
            }

            return new LocalizedValue<TProp>(result, requestLanguage, currentLanguage);
        }

        public string GetLocalizedModuleProperty(IModuleDescriptor module, string propertyName, int languageId = 0, bool doFallback = true)
        {
            Guard.NotNull(module);
            Guard.NotEmpty(propertyName);

            var systemName = module.SystemName;
            var resourceName = string.Format("Plugins.{0}.{1}", propertyName, systemName);
            var result = _localizationService.GetResource(resourceName, languageId, logIfNotFound: false, returnEmptyIfNotFound: true);

            if (string.IsNullOrEmpty(result) && doFallback)
            {
                var prop = module.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    result = prop.GetValue(module) as string;
                }
            }

            return result;
        }

        public virtual string GetActiveSlug(
            string entityName,
            int entityId,
            int? languageId,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true)
        {
            return GetActiveSlugAsync(entityName, entityId, languageId, returnDefaultValue, ensureTwoPublishedLanguages).Await();
        }

        public virtual async Task<string> GetActiveSlugAsync(
            string entityName,
            int entityId,
            int? languageId,
            bool returnDefaultValue = true,
            bool ensureTwoPublishedLanguages = true)
        {
            string result = string.Empty;

            languageId ??= _workContext.WorkingLanguage.Id;

            if (languageId > 0)
            {
                // Ensure that we have at least two published languages
                bool loadLocalizedValue = true;
                if (ensureTwoPublishedLanguages)
                {
                    loadLocalizedValue = LanguageCount > 1;
                }

                // Localized value
                if (loadLocalizedValue)
                {
                    result = await _urlService.GetActiveSlugAsync(entityId, entityName, languageId.Value);
                }
            }

            // Set default value if required
            if (string.IsNullOrEmpty(result) && returnDefaultValue)
            {
                result = await _urlService.GetActiveSlugAsync(entityId, entityName, 0);
            }

            return result;
        }

        // TODO (mg): Insufficient method(s). The caller must use IUrlHelperUrl.RouteUrl to create URLs.
        public virtual async Task<LocalizedLinkEntry[]> GetLocalizedLinkEntriesAsync(string entityName, int entityId, Store store = null)
        {
            Guard.NotEmpty(entityName);
            Guard.NotZero(entityId);

            if (!_seoSettings.AddAlternateHtmlLinks)
            {
                return null;
            }

            store ??= _storeContext.CurrentStore;

            var allLanguages = await _languageService.GetAllLanguagesAsync(false, store.Id);
            var defaultLanguageId = await _languageService.GetMasterLanguageIdAsync(store.Id);
            var languageIds = allLanguages.Select(x => x.Id).Concat([0]).ToArray();
            var slugs = await _urlService.GetUrlRecordCollectionAsync(entityName, languageIds, [entityId]);

            // INFO: Also add an alternate link:
            // - For current language: https://developers.google.com/search/docs/specialty/international/localized-versions?visit_id=638802176426773299-2509641004#html
            // - Even if there is only one language published. The admin can suppress it by deactivating AddAlternateHtmlLinks.
            return [.. allLanguages
                .Select(lang =>
                {
                    var slug = slugs.GetSlug(lang.Id, entityId, lang.Id == defaultLanguageId);
                    if (slug != null)
                    {
                        var baseUrl = GetBaseUrl(lang, store);
                        return new LocalizedLinkEntry
                        {
                            Lang = lang.LanguageCulture,
                            Href = baseUrl + RouteHelper.NormalizePathComponent(slug.EmptyNull().TrimStart('/'))
                        };
                    }

                    return null;
                })
                .Where(x => x != null)];
        }

        private string GetBaseUrl(Language language, Store store)
        {
            var baseUrl = store.GetBaseUrl();
            var localizationSettings = _settingFactory.LoadSettings<LocalizationSettings>(store.Id);

            if (localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                var defaultLangId = _languageService.GetMasterLanguageId(store.Id);
                if (language.Id != defaultLangId || localizationSettings.DefaultLanguageRedirectBehaviour < DefaultLanguageRedirectBehaviour.StripSeoCode)
                {
                    baseUrl += language.GetTwoLetterISOLanguageName() + '/';
                }
            }

            return baseUrl;
        }
    }
}