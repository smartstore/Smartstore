using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Data.Batching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Smartstore.Core.Localization
{
    public class LocalizationService : AsyncDbSaveHook<LocaleStringResource>, ILocalizationService
    {
        /// <summary>
        /// 0 = language id
        /// </summary>
        const string CACHE_SEGMENT_KEY = "localization:{0}";
        const string CACHE_SEGMENT_PATTERN = "localization:*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;

        private int _notFoundLogCount = 0;
        private int? _defaultLanguageId;

        public LocalizationService(
            SmartDbContext db, 
            ICacheManager cache,
            IWorkContext workContext,
            ILanguageService languageService)
        {
            _db = db;
            _cache = cache;
            _workContext = workContext;
            _languageService = languageService;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #region Cache & Hook

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            await ClearCacheSegmentAsync((entry.Entity as LocaleStringResource).LanguageId);
            return HookResult.Ok;
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
            languageId = languageId > 0 ? languageId : _workContext.WorkingLanguage?.Id ?? 0;
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
                        // TODO: (core) I NEVER wanted to do this (.Result), but I don't wanna always repeat myself either.
                        _defaultLanguageId = _languageService.GetDefaultLanguageIdAsync().Result;
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
            languageId = languageId > 0 ? languageId : _workContext.WorkingLanguage?.Id ?? 0;
            if (languageId == 0)
            {
                return defaultValue;
            }   

            resourceKey = resourceKey.EmptyNull().Trim().ToLowerInvariant();

            var cachedSegment = await GetCacheSegmentAsync(languageId).ConfigureAwait(false);
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
                        _defaultLanguageId = await _languageService.GetDefaultLanguageIdAsync().ConfigureAwait(false);
                    }

                    var defaultLangId = _defaultLanguageId.Value;
                    if (defaultLangId > 0 && defaultLangId != languageId)
                    {
                        var fallbackResult = await GetResourceAsync(resourceKey, defaultLangId, false, resourceKey).ConfigureAwait(false);
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
            if (_workContext.WorkingLanguage != null)
            {
                return GetLocaleStringResourceByNameAsync(resourceName, _workContext.WorkingLanguage.Id);
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
                result = await _db.LocaleStringResources.Where(x => EF.Functions.Like(x.ResourceName, pattern)).BatchDeleteAsync();
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
