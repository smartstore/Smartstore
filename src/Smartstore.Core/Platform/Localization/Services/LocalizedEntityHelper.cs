using System.Globalization;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
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
        private readonly IWorkContext _workContext;

        private readonly int _languageCount;
        private readonly Language _masterLanguage;

        public LocalizedEntityHelper(
            SmartDbContext db,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            ILocalizationService localizationService,
            IUrlService urlService,
            IWorkContext workContext)
        {
            _db = db;
            _languageService = languageService;
            _localizedEntityService = localizedEntityService;
            _localizationService = localizationService;
            _urlService = urlService;
            _workContext = workContext;

            _languageCount = _languageService.GetAllLanguages().Count();
            _masterLanguage = _db.Languages.FindById(_languageService.GetMasterLanguageId());
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
                loadLocalizedValue = _languageCount > 1;
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
                currentLanguage = _masterLanguage;
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

            if (languageId == null)
            {
                languageId = _workContext.WorkingLanguage.Id;
            }

            if (languageId > 0)
            {
                // Ensure that we have at least two published languages
                bool loadLocalizedValue = true;
                if (ensureTwoPublishedLanguages)
                {
                    loadLocalizedValue = _languageCount > 1;
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
    }
}