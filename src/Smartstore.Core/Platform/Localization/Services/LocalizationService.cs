using Humanizer;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    public partial class LocalizationService : AsyncDbSaveHook<LocaleStringResource>, ILocalizationService
    {
        /// <summary>
        /// 0 = language id
        /// </summary>
        const string CACHE_SEGMENT_KEY = "localization:{0}";
        const string CACHE_SEGMENT_PATTERN = "localization:*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly Lazy<IWorkContext> _workContext;
        private readonly ILanguageService _languageService;

        private int _notFoundLogCount = 0;
        private int? _defaultLanguageId;

        public LocalizationService(
            SmartDbContext db,
            ICacheManager cache,
            Lazy<IWorkContext> workContext,
            ILanguageService languageService)
        {
            _db = db;
            _cache = cache;
            _workContext = workContext;
            _languageService = languageService;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Cache & Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var langIds = entries
                .Select(x => x.Entity)
                .OfType<LocaleStringResource>()
                .Select(x => x.LanguageId)
                .Distinct()
                .ToArray();

            foreach (var langId in langIds)
            {
                await ClearCacheSegmentAsync(langId);
            }
        }

        protected virtual Dictionary<string, string> GetCacheSegment(int languageId)
        {
            var cacheKey = BuildCacheSegmentKey(languageId);

            return _cache.Get(cacheKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var resources = _db.LocaleStringResources
                    .Where(x => x.LanguageId == languageId)
                    .Select(x => new { x.ResourceName, x.ResourceValue })
                    .ToList();

                var dict = new Dictionary<string, string>(resources.Count);

                foreach (var res in resources)
                {
                    dict[res.ResourceName.ToLowerInvariant()] = res.ResourceValue;
                }

                return dict;
            });
        }

        protected virtual async Task<Dictionary<string, string>> GetCacheSegmentAsync(int languageId)
        {
            var cacheKey = BuildCacheSegmentKey(languageId);

            return await _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var resources = await _db.LocaleStringResources
                    .Where(x => x.LanguageId == languageId)
                    .Select(x => new { x.ResourceName, x.ResourceValue })
                    .ToListAsync();

                var dict = new Dictionary<string, string>(resources.Count);

                foreach (var res in resources)
                {
                    dict[res.ResourceName.ToLowerInvariant()] = res.ResourceValue;
                }

                return dict;
            });
        }

        /// <summary>
        /// Clears the cached resource segment from the cache
        /// </summary>
        /// <param name="languageId">Language Id. If <c>null</c>, segments for all cached languages will be invalidated</param>
        protected virtual Task ClearCacheSegmentAsync(int? languageId = null)
        {
            if (languageId.HasValue && languageId.Value > 0)
            {
                return _cache.RemoveAsync(BuildCacheSegmentKey(languageId.Value));
            }
            else
            {
                return _cache.RemoveByPatternAsync(CACHE_SEGMENT_PATTERN);
            }
        }

        protected virtual string BuildCacheSegmentKey(int languageId)
        {
            return string.Format(CACHE_SEGMENT_KEY, languageId);
        }

        #endregion

        #region LocaleStringResources

        public virtual string GetResource(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false)
        {
            languageId = languageId > 0 ? languageId : _workContext.Value.WorkingLanguage?.Id ?? 0;
            if (languageId == 0)
            {
                return defaultValue;
            }

            resourceKey = resourceKey.EmptyNull().Trim().ToLowerInvariant();

            var cachedSegment = GetCacheSegment(languageId);
            if (!cachedSegment.TryGetValue(resourceKey, out string result))
            {
                if (logIfNotFound)
                {
                    LogNotFound(resourceKey, languageId);
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    result = defaultValue;
                }
                else
                {
                    // Try fallback to default language
                    if (!_defaultLanguageId.HasValue)
                    {
                        _defaultLanguageId = _languageService.GetMasterLanguageId();
                    }

                    var defaultLangId = _defaultLanguageId.Value;
                    if (defaultLangId > 0 && defaultLangId != languageId)
                    {
                        var fallbackResult = GetResource(resourceKey, defaultLangId, false, resourceKey);
                        if (fallbackResult != resourceKey)
                        {
                            result = fallbackResult;
                        }
                    }

                    if (!returnEmptyIfNotFound && result.IsEmpty())
                    {
                        result = resourceKey;
                    }
                }
            }

            return result;
        }

        public virtual async Task<string> GetResourceAsync(string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false)
        {
            languageId = languageId > 0 ? languageId : _workContext.Value.WorkingLanguage?.Id ?? 0;
            if (languageId == 0)
            {
                return defaultValue;
            }

            resourceKey = resourceKey.EmptyNull().Trim().ToLowerInvariant();

            var cachedSegment = await GetCacheSegmentAsync(languageId);
            if (!cachedSegment.TryGetValue(resourceKey, out string result))
            {
                if (logIfNotFound)
                {
                    LogNotFound(resourceKey, languageId);
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    result = defaultValue;
                }
                else
                {
                    // Try fallback to default language
                    if (!_defaultLanguageId.HasValue)
                    {
                        _defaultLanguageId = await _languageService.GetMasterLanguageIdAsync();
                    }

                    var defaultLangId = _defaultLanguageId.Value;
                    if (defaultLangId > 0 && defaultLangId != languageId)
                    {
                        var fallbackResult = await GetResourceAsync(resourceKey, defaultLangId, false, resourceKey);
                        if (fallbackResult != resourceKey)
                        {
                            result = fallbackResult;
                        }
                    }

                    if (!returnEmptyIfNotFound && result.IsEmpty())
                    {
                        result = resourceKey;
                    }
                }
            }

            return result;
        }

        public virtual string GetLocalizedEnum<T>(T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            Guard.IsEnumType(typeof(T), nameof(enumValue));

            var enumName = typeof(T).GetAttribute<EnumAliasNameAttribute>(false)?.Name ?? typeof(T).Name;
            var resourceName = $"Enums.{enumName}.{enumValue}";

            if (hint)
            {
                resourceName += ".Hint";
            }

            var result = GetResource(resourceName, languageId, logIfNotFound: false, returnEmptyIfNotFound: true);

            // Set default value if required.
            if (string.IsNullOrEmpty(result))
            {
                result = enumValue.ToString().Titleize();
            }

            return result;
        }

        public virtual async Task<string> GetLocalizedEnumAsync<T>(T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            Guard.IsEnumType(typeof(T), nameof(enumValue));

            var enumName = typeof(T).GetAttribute<EnumAliasNameAttribute>(false)?.Name ?? typeof(T).Name;
            var resourceName = $"Enums.{enumName}.{enumValue}";

            if (hint)
            {
                resourceName += ".Hint";
            }

            var result = await GetResourceAsync(resourceName, languageId, logIfNotFound: false, returnEmptyIfNotFound: true);

            // Set default value if required.
            if (string.IsNullOrEmpty(result))
            {
                result = enumValue.ToString().Titleize();
            }

            return result;
        }

        private void LogNotFound(string resourceKey, int languageId)
        {
            if (_notFoundLogCount < 50)
            {
                Logger.Warn("Resource string ({0}) does not exist. Language ID = {1}", resourceKey, languageId);
            }
            else if (_notFoundLogCount == 50)
            {
                Logger.Warn("Too many language resources do not exist (> 50). Stopped logging missing resources to prevent performance drop.");
            }

            _notFoundLogCount++;
        }

        public virtual Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName)
        {
            if (_workContext.Value.WorkingLanguage != null)
            {
                return GetLocaleStringResourceByNameAsync(resourceName, _workContext.Value.WorkingLanguage.Id);
            }

            return Task.FromResult((LocaleStringResource)null);
        }

        public virtual async Task<LocaleStringResource> GetLocaleStringResourceByNameAsync(string resourceName, int languageId, bool logIfNotFound = true)
        {
            var query = from x in _db.LocaleStringResources
                        orderby x.ResourceName
                        where x.LanguageId == languageId && x.ResourceName == resourceName
                        select x;

            var entity = await query.FirstOrDefaultAsync();

            if (logIfNotFound && entity == null)
            {
                Logger.Warn("Resource string ({0}) not found. Language ID = {1}", resourceName, languageId);
            }

            return entity;
        }

        public virtual async Task<int> DeleteLocaleStringResourcesAsync(string key, bool keyIsRootKey = true)
        {
            if (key.IsEmpty())
            {
                return 0;
            }

            int result = 0;

            try
            {
                var pattern = (key.EndsWith(".") || !keyIsRootKey ? key : key + ".") + "%";
                result = await _db.LocaleStringResources.Where(x => EF.Functions.Like(x.ResourceName, pattern)).ExecuteDeleteAsync();
                await ClearCacheSegmentAsync(null);
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return result;
        }

        #endregion
    }
}