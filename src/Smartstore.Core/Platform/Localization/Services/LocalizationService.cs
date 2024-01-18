using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Humanizer;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    public partial class LocalizationService : AsyncDbSaveHook<LocaleStringResource>, ILocalizationService, IDisposable
    {
        /// <summary>
        /// 0 = language id
        /// </summary>
        const string CACHE_SEGMENT_KEY = "localization:{0}";
        const string CACHE_SEGMENT_PATTERN = "localization:*";

        const string EnumPrefix = "Enums.";
        const string HintSuffix = ".Hint";

        private static readonly ConcurrentDictionary<Type, string> _enumNames = new();
        private static readonly ConcurrentDictionary<string, byte> _missingResKeys = new(StringComparer.OrdinalIgnoreCase);

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly Lazy<IWorkContext> _workContext;
        private readonly ILanguageService _languageService;
        private bool? _isMultiLanguageEnvironment;

        // Scope cache
        private int? _masterLanguageId;
        private Dictionary<string, string> _singleCacheSegment;
        private Dictionary<int, Dictionary<string, string>> _cacheSegments;
        private readonly HashSet<(string Key, int LangId)> _missedKeysInScope = new();

        private bool IsMultiLanguageEnvironment
        {
            get => _isMultiLanguageEnvironment ??= _languageService.IsMultiLanguageEnvironment();
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual Dictionary<string, string> GetCacheSegment(int languageId)
        {
            // Perf (hot path): first try faster lookup in request scope, then access cache.
            if (!IsMultiLanguageEnvironment)
            {
                if (_singleCacheSegment != null)
                {
                    return _singleCacheSegment;
                }
            }
            else
            {
                if (_cacheSegments != null && _cacheSegments.TryGetValue(languageId, out var segment))
                {
                    return segment;
                }
            }
            
            var cacheKey = BuildCacheSegmentKey(languageId);

            var cacheSegment = _cache.Get(cacheKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var resources = _db.LocaleStringResources
                    .Where(x => x.LanguageId == languageId)
                    .Select(x => new { x.ResourceName, x.ResourceValue })
                    .ToList();

                var dict = new Dictionary<string, string>(resources.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var res in resources)
                {
                    dict[res.ResourceName] = res.ResourceValue;
                }

                return dict;
            });

            // Put resolved segment to scope cache
            if (!IsMultiLanguageEnvironment)
            {
                _singleCacheSegment = cacheSegment;
            }
            else
            {
                _cacheSegments ??= new();
                _cacheSegments[languageId] = cacheSegment;
            }

            return cacheSegment;
        }

        /// <summary>
        /// Clears the cached resource segment from the cache
        /// </summary>
        /// <param name="languageId">Language Id. If <c>null</c>, segments for all cached languages will be invalidated</param>
        protected virtual Task ClearCacheSegmentAsync(int? languageId = null)
        {
            _singleCacheSegment = null;
            
            if (languageId.HasValue && languageId.Value > 0)
            {
                _cacheSegments?.Remove(languageId.Value);
                return _cache.RemoveAsync(BuildCacheSegmentKey(languageId.Value));
            }
            else
            {
                _cacheSegments = null;
                return _cache.RemoveByPatternAsync(CACHE_SEGMENT_PATTERN);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual string BuildCacheSegmentKey(int languageId)
        {
            return string.Format(CACHE_SEGMENT_KEY, languageId);
        }

        #endregion

        #region LocaleStringResources

        public virtual string GetResource(
            string resourceKey,
            int languageId = 0,
            bool logIfNotFound = true,
            string defaultValue = "",
            bool returnEmptyIfNotFound = false)
        {
            if (string.IsNullOrEmpty(resourceKey))
            {
                return string.Empty;
            }
            
            if (languageId <= 0)
            {
                languageId = _workContext.Value.WorkingLanguage?.Id ?? 0;
                if (languageId == 0)
                {
                    return defaultValue;
                }
            }

            // Trim whitespace (avoid string allocations if not necessary)
            var trim = char.IsWhiteSpace(resourceKey[0]) || (resourceKey.Length > 1 && char.IsWhiteSpace(resourceKey[^1]));
            if (trim)
            {
                resourceKey = resourceKey.Trim();
            }

            var cacheSegment = GetCacheSegment(languageId);

            if (cacheSegment.TryGetValue(resourceKey, out string result))
            {
                return result;
            }

            if (logIfNotFound)
            {
                HandleMissingResource(resourceKey, languageId);
            }

            if (!string.IsNullOrEmpty(defaultValue))
            {
                result = defaultValue;
            }
            else
            {
                if (IsMultiLanguageEnvironment)
                {
                    // Try fallback to default/master language
                    if (!_masterLanguageId.HasValue)
                    {
                        _masterLanguageId = _languageService.GetMasterLanguageId();
                    }

                    var masterLangId = _masterLanguageId.Value;
                    if (masterLangId > 0 && masterLangId != languageId)
                    {
                        var fallbackResult = GetResource(resourceKey, masterLangId, false, resourceKey);
                        if (fallbackResult != resourceKey)
                        {
                            result = fallbackResult;
                        }
                    }
                }

                if (!returnEmptyIfNotFound && result.IsEmpty())
                {
                    result = resourceKey;
                }
            }

            return result;
        }

        public virtual string GetLocalizedEnum<T>(T enumValue, int languageId = 0, bool hint = false)
            where T : struct
        {
            Guard.IsEnumType(typeof(T));

            var enumName = _enumNames.GetOrAdd(typeof(T), type =>
            {
                return type.GetAttribute<EnumAliasNameAttribute>(false)?.Name ?? type.Name;
            });

            var enumValueStr = enumValue.ToString();
            var resourceName = EnumPrefix + enumName + '.' + enumValueStr;
            if (hint)
            {
                resourceName += HintSuffix;
            }

            var result = GetResource(
                resourceName, 
                languageId, 
                logIfNotFound: false, 
                returnEmptyIfNotFound: true);

            // Set default value if required.
            if (string.IsNullOrEmpty(result))
            {
                result = enumValueStr.Titleize();
            }

            return result;
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

        private void HandleMissingResource(string resourceKey, int languageId)
        {
            var missKey = resourceKey + '.' + languageId.ToStringInvariant();

            if (_missingResKeys.ContainsKey(missKey))
            {
                // Has been logged already, don't warn again.
                return;
            }

            _missingResKeys.TryAdd(missKey, 1);
            _missedKeysInScope.Add((resourceKey, languageId));
        }

        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        void IDisposable.Dispose()
        {
            if (_missedKeysInScope.Count == 1)
            {
                var missedKey = _missedKeysInScope.First();
                Logger.Warn("Resource string {0} does not exist. Language ID = {1}.", missedKey.Key, missedKey.LangId);
            }
            else if (_missedKeysInScope.Count > 1)
            {
                var title = $"{_missedKeysInScope.Count} resource strings are missing. See full message for details.";
                var lines = _missedKeysInScope.Take(100).Select(x => $"{x.Key} (Language ID = {x.LangId})");
                var fullMessage =
                    "Missing resource strings (max. 100 items):" +
                    Environment.NewLine +
                    "------------------------------------------------------------------" +
                    Environment.NewLine +
                    string.Join(Environment.NewLine, lines);

                Logger.Warn(new Exception(fullMessage), title);
            }

            _missedKeysInScope.Clear();
        }
    }
}