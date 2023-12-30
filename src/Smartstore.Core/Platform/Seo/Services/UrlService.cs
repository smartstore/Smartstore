using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Smartstore.Caching;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Threading;

namespace Smartstore.Core.Seo
{
    public partial class UrlService : AsyncDbSaveHook<UrlRecord>, IUrlService
    {
        /// <summary>
        /// 0 = segment (EntityName.IdRange), 1 = language id
        /// </summary>
        private readonly static CompositeFormat URLRECORD_SEGMENT_KEY = CompositeFormat.Parse("urlrecord:segment:{0}-lang-{1}");
        const string URLRECORD_SEGMENT_PATTERN = "urlrecord:segment:{0}*";
        const string URLRECORD_ALL_ACTIVESLUGS_KEY = "urlrecord:all-active-slugs";

        internal readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ILanguageService _languageService;
        private readonly LocalizationSettings _localizationSettings;
        internal readonly SeoSettings _seoSettings;
        internal readonly IRouteHelper _routeHelper;
        private readonly PerformanceSettings _performanceSettings;
        private readonly SecuritySettings _securitySettings;

        internal IDictionary<string, UrlRecord> _extraSlugLookup;
        private IDictionary<string, UrlRecordCollection> _prefetchedCollections;
        private static int _lastCacheSegmentSize = -1;

        public UrlService(
            SmartDbContext db,
            ICacheManager cache,
            IHttpContextAccessor httpContextAccessor,
            IWorkContext workContext,
            IStoreContext storeContext,
            ILanguageService languageService,
            IRouteHelper routeHelper,
            LocalizationSettings localizationSettings,
            SeoSettings seoSettings,
            PerformanceSettings performanceSettings,
            SecuritySettings securitySettings)
        {
            _db = db;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
            _workContext = workContext;
            _storeContext = storeContext;
            _languageService = languageService;
            _routeHelper = routeHelper;
            _localizationSettings = localizationSettings;
            _seoSettings = seoSettings;
            _performanceSettings = performanceSettings;
            _securitySettings = securitySettings;

            _prefetchedCollections = new Dictionary<string, UrlRecordCollection>(StringComparer.OrdinalIgnoreCase);
            _extraSlugLookup = new Dictionary<string, UrlRecord>();

            ValidateCacheState();
        }

        internal UrlService GetInstanceForBatching(SmartDbContext db = null)
        {
            if (db == null || db == _db)
            {
                return this;
            }

            return new UrlService(db,
                _cache,
                _httpContextAccessor,
                _workContext,
                _storeContext,
                _languageService,
                _routeHelper,
                _localizationSettings,
                _seoSettings,
                _performanceSettings,
                _securitySettings)
            {
                _extraSlugLookup = _extraSlugLookup,
                _prefetchedCollections = _prefetchedCollections
            };
        }

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var distinctEntries = entries
                .Select(x => x.Entity)
                .OfType<UrlRecord>()
                .Select(x => (x.EntityName, x.EntityId, x.LanguageId))
                .Distinct()
                .ToArray();

            foreach (var entry in distinctEntries)
            {
                await ClearCacheSegmentAsync(entry.EntityName, entry.EntityId, entry.LanguageId);
            }

            _extraSlugLookup.Clear();
        }

        #endregion

        #region Cache

        /// <summary>
        /// Gets the cache segment for a entity name, entity id and language id combination.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="entityId"></param>
        /// <param name="languageId"></param>
        /// <returns></returns>
        protected virtual async Task<Dictionary<int, string>> GetCacheSegmentAsync(string entityName, int entityId, int languageId)
        {
            Guard.NotEmpty(entityName);

            var segmentKey = GetSegmentKeyPart(entityName, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey, languageId);

            return await _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                var query = from ur in _db.UrlRecords.AsNoTracking()
                            where
                                ur.EntityId >= minEntityId &&
                                ur.EntityId <= maxEntityId &&
                                ur.EntityName == entityName &&
                                ur.LanguageId == languageId &&
                                ur.IsActive
                            orderby ur.Id descending
                            select ur;

                var urlRecords = await query.ToListAsync();

                var dict = new Dictionary<int, string>(urlRecords.Count);

                foreach (var ur in urlRecords)
                {
                    dict[ur.EntityId] = ur.Slug.EmptyNull();
                }

                return dict;
            }, independent: true);
        }

        /// <summary>
        /// Clears the cached segment from the cache
        /// </summary>
        protected virtual async Task ClearCacheSegmentAsync(string entityName, int entityId, int? languageId = null)
        {
            var segmentKey = GetSegmentKeyPart(entityName, entityId);

            if (languageId > 0)
            {
                await _cache.RemoveAsync(BuildCacheSegmentKey(segmentKey, languageId.Value));
            }
            else
            {
                await _cache.RemoveByPatternAsync(URLRECORD_SEGMENT_PATTERN.FormatInvariant(segmentKey));
            }

            // Always delete this (in case when LoadAllOnStartup is true)
            await _cache.RemoveAsync(URLRECORD_ALL_ACTIVESLUGS_KEY);
        }

        private void ValidateCacheState()
        {
            // Ensure that after a segment size change the cache segments are invalidated.
            var size = _performanceSettings.CacheSegmentSize;
            var changed = _lastCacheSegmentSize == -1;

            if (size <= 0)
            {
                _performanceSettings.CacheSegmentSize = size = 1;
            }

            if (_lastCacheSegmentSize > 0 && _lastCacheSegmentSize != size)
            {
                _cache.RemoveByPattern(URLRECORD_SEGMENT_PATTERN);
                changed = true;
            }

            if (changed)
            {
                Interlocked.Exchange(ref _lastCacheSegmentSize, size);
            }
        }

        private static string BuildCacheSegmentKey(string segment, int languageId)
        {
            return URLRECORD_SEGMENT_KEY.FormatInvariant(segment, languageId);
        }

        private string GetSegmentKeyPart(string entityName, int entityId)
        {
            return GetSegmentKeyPart(entityName, entityId, out _, out _);
        }

        private string GetSegmentKeyPart(string entityName, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(_performanceSettings.CacheSegmentSize);
            return (entityName + "." + minId.ToString()).ToLowerInvariant();
        }

        private static string GenerateKey(int entityId, string entityName, int languageId)
        {
            return entityId.ToStringInvariant() + '.' + entityName + '.' + languageId.ToStringInvariant();
        }

        #endregion

        #region IUrlService

        public virtual UrlPolicy GetUrlPolicy()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                throw new InvalidOperationException("A valid HttpContext instance is required for successful URL policy creation.");
            }

            var policy = httpContext.GetUrlPolicy();
            if (policy == null)
            {
                throw new InvalidOperationException("URL policy cannot be resolved. 'UseLocalizedRouting' middleware should have been run before calling this method.");
            }

            return policy;
        }

        public virtual IUrlServiceBatchScope CreateBatchScope(SmartDbContext db = null)
        {
            return new UrlServiceBatchScope(this, db);
        }

        public virtual async Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId)
        {
            Guard.NotEmpty(entityName);

            if (TryGetPrefetchedActiveSlug(entityId, entityName, languageId, out var slug))
            {
                return slug;
            }

            if (_seoSettings.LoadAllUrlAliasesOnStartup)
            {
                var allActiveSlugs = await _cache.GetAsync(URLRECORD_ALL_ACTIVESLUGS_KEY, async (o) =>
                {
                    o.ExpiresIn(TimeSpan.FromHours(8));

                    var query = from x in _db.UrlRecords.AsNoTracking()
                                where x.IsActive
                                orderby x.Id descending
                                select x;

                    var items = await query.ToListAsync();
                    var result = items.ToDictionarySafe(
                        x => GenerateKey(x.EntityId, x.EntityName, x.LanguageId),
                        x => x.Slug,
                        StringComparer.OrdinalIgnoreCase);

                    return result;
                }, independent: true);

                var key = GenerateKey(entityId, entityName, languageId);
                if (!allActiveSlugs.TryGetValue(key, out slug))
                {
                    return string.Empty;
                }
            }
            else
            {
                var slugs = await GetCacheSegmentAsync(entityName, entityId, languageId);

                if (!slugs.TryGetValue(entityId, out slug))
                {
                    return string.Empty;
                }
            }

            return slug;
        }

        public virtual async Task PrefetchUrlRecordsAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            var collection = await GetUrlRecordCollectionAsync(entityName, languageIds, entityIds, isRange, isSorted, tracked);

            if (_prefetchedCollections.TryGetValue(entityName, out var existing))
            {
                collection.MergeWith(existing);
            }
            else
            {
                _prefetchedCollections[entityName] = collection;
            }
        }

        public virtual void ClearPrefetchCache()
        {
            _prefetchedCollections.Clear();
        }

        public virtual async Task<UrlRecordCollection> GetUrlRecordCollectionAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            Guard.NotEmpty(entityName);

            var query = from x in _db.UrlRecords.ApplyTracking(tracked)
                        where x.EntityName == entityName && x.IsActive
                        select x;

            var requestedSet = entityIds;

            if (entityIds != null && entityIds.Length > 0)
            {
                if (isRange)
                {
                    if (!isSorted)
                    {
                        Array.Sort(entityIds);
                    }

                    var min = entityIds[0];
                    var max = entityIds[entityIds.Length - 1];

                    if (entityIds.Length == 2 && max > min + 1)
                    {
                        // Only min & max were passed, create the range sequence.
                        requestedSet = Enumerable.Range(min, max - min + 1).ToArray();
                    }

                    query = query.Where(x => x.EntityId >= min && x.EntityId <= max);
                }
                else
                {
                    requestedSet = entityIds;
                    query = query.Where(x => entityIds.Contains(x.EntityId));
                }
            }

            if (languageIds != null && languageIds.Length > 0)
            {
                if (languageIds.Length == 1)
                {
                    // Avoid "The LINQ expression node type 'ArrayIndex' is not supported in LINQ to Entities".
                    var languageId = languageIds[0];
                    query = query.Where(x => x.LanguageId == languageId);
                }
                else
                {
                    query = query.Where(x => languageIds.Contains(x.LanguageId));
                }
            }

            // Don't sort DESC, because latter items overwrite exisiting ones (it's the same as sorting DESC and taking the first)
            var items = await query.OrderBy(x => x.Id).ToListAsync();
            return new UrlRecordCollection(entityName, requestedSet, items);
        }

        protected internal bool TryGetPrefetchedActiveSlug(int entityId, string entityName, int languageId, out string slug)
        {
            slug = null;

            if (_prefetchedCollections.TryGetValue(entityName, out var collection))
            {
                var record = collection.Find(languageId, entityId);
                if (record != null)
                {
                    slug = record.Slug.NullEmpty();
                }
            }

            return slug != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<UrlRecord> ApplySlugAsync(ValidateSlugResult result, bool save = false)
        {
            return ApplySlugAsync(result, _prefetchedCollections.Get(result.EntityName), save);
        }

        protected internal virtual async Task<UrlRecord> ApplySlugAsync(ValidateSlugResult result, UrlRecordCollection prefetchedCollection, bool save = false)
        {
            if (!result.WasValidated)
            {
                throw new ArgumentException("Unvalidated slugs cannot be applied. Consider obtaining 'ValidateSlugResult' from 'ValidateSlugAsync()' method.", nameof(result));
            }

            var dirty = false;
            var entry = result.Found;
            var languageId = result.LanguageId ?? 0;

            if (string.IsNullOrWhiteSpace(result.Slug))
            {
                // Disable the previous active URL record.
                var currentActive = await GetActiveEntryFromStoreAsync();
                if (currentActive != null)
                {
                    dirty = true;
                    currentActive.IsActive = false;
                }
            }
            else
            {
                if (entry != null && result.FoundIsSelf)
                {
                    // Found record refers to requested entity
                    if (entry.IsActive)
                    {
                        // ...and is active. Do nothing, 'cause nothing changed.
                    }
                    else
                    {
                        // ...and is inactive. Make it active
                        entry.IsActive = true;
                        dirty = true;

                        // ...and make the current active one(s) inactive.
                        var currentActive = await GetActiveEntryFromStoreAsync();
                        if (currentActive != null)
                        {
                            currentActive.IsActive = false;
                        }
                    }
                }

                if (entry == null || !result.FoundIsSelf)
                {
                    // Disable the previous active URL record.
                    var currentActive = await GetActiveEntryFromStoreAsync();
                    if (currentActive != null)
                    {
                        currentActive.IsActive = false;
                    }

                    // Create new entry because no entry was found or found one refers to another entity.
                    // Because unvalidated slugs cannot be passed to this method we assume slug uniqueness.
                    entry = new UrlRecord
                    {
                        EntityId = result.Source.Id,
                        EntityName = result.EntityName,
                        Slug = result.Slug,
                        LanguageId = languageId,
                        IsActive = true,
                    };
                }

                if (entry != null && entry.IsTransientRecord())
                {
                    // It's a freshly created record, add to set.
                    _db.UrlRecords.Add(entry);

                    // When we gonna save deferred, adding the new entry to our extra lookup
                    // will ensure that subsequent validation does not miss new records.
                    _extraSlugLookup[entry.Slug] = entry;

                    dirty = true;
                }
            }

            if (dirty && save)
            {
                await _db.SaveChangesAsync();
            }

            return entry;

            async Task<UrlRecord> GetActiveEntryFromStoreAsync()
            {
                if (result.Source.Id > 0)
                {
                    if (prefetchedCollection != null)
                    {
                        var record = prefetchedCollection.Find(languageId, result.Source.Id);
                        if (record != null)
                        {
                            // Transient: was requested from store, but does not exist.
                            return record.IsTransientRecord() ? null : record;
                        }
                    }

                    return await _db.UrlRecords
                        .ApplyEntityFilter(result.Source, languageId, true)
                        .FirstOrDefaultAsync();
                }

                return null;
            }
        }

        public virtual async ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(T entity,
            string seName,
            string displayName,
            bool ensureNotEmpty,
            int? languageId = null,
            bool force = false)
            where T : ISlugSupported
        {
            Guard.NotNull(entity);

            // Use displayName if seName is not specified.
            if (string.IsNullOrWhiteSpace(seName) && !string.IsNullOrWhiteSpace(displayName))
            {
                seName = displayName;
            }

            // Validation
            var slug = SlugUtility.Slugify(seName,
                _seoSettings.ConvertNonWesternChars,
                _seoSettings.AllowUnicodeCharsInUrls,
                true,
                _seoSettings.GetCharConversionMap());

            if (string.IsNullOrWhiteSpace(slug))
            {
                if (ensureNotEmpty)
                {
                    // Use entity identifier as slug if empty
                    slug = entity.GetEntityName().ToLower() + entity.Id.ToStringInvariant();
                }
                else
                {
                    // Return. no need for further processing
                    return new ValidateSlugResult
                    {
                        Source = entity,
                        Slug = slug,
                        LanguageId = languageId,
                        WasValidated = true
                    };
                }
            }

            // Validate and alter slug if it could be interpreted as SEO code
            if (CultureHelper.IsValidCultureCode(slug))
            {
                if (seName.Length == 2)
                {
                    slug += "-0";
                }
            }

            // Ensure this slug is not reserved
            int i = 2;
            string tempSlug = slug;
            UrlRecord found = null;
            bool foundIsSelf = false;

            while (true)
            {
                // Check whether such slug already exists in the database
                var urlRecord = (!force ? _extraSlugLookup.Get(tempSlug) : null) ?? await _db.UrlRecords.FirstOrDefaultAsync(x => x.Slug == tempSlug);

                // Check whether found record refers to requested entity
                foundIsSelf = FoundRecordIsSelf(entity, urlRecord, languageId);

                // ...and it's not in the list of reserved slugs
                var reserved = _routeHelper.IsReservedPath(tempSlug, out var partialMatch);

                if ((urlRecord == null || foundIsSelf) && !reserved)
                {
                    found = urlRecord;
                    break;
                }

                if (reserved && partialMatch.HasValue())
                {
                    // Strip off prefix and continue with substring
                    slug = tempSlug = slug[(partialMatch.Length + 1)..];
                    continue;
                }

                // Try again with unique index appended
                var suffixLen = Math.Floor(Math.Log10(i) + 1).Convert<int>() + 1;
                tempSlug = CompositeFormatCache.Get("{0}-{1}").FormatInvariant(slug.Truncate(400 - suffixLen), i);
                found = urlRecord;
                i++;
            }
            slug = tempSlug;

            return new ValidateSlugResult
            {
                Source = entity,
                Slug = slug,
                Found = found,
                FoundIsSelf = foundIsSelf,
                LanguageId = languageId,
                WasValidated = true
            };
        }

        public virtual Task<Dictionary<int, int>> CountSlugsPerEntityAsync(params int[] urlRecordIds)
        {
            if (urlRecordIds.Length == 0)
                return Task.FromResult(new Dictionary<int, int>());

            var query =
                from x in _db.UrlRecords
                where urlRecordIds.Contains(x.Id)
                select new
                {
                    x.Id,
                    Count = _db.UrlRecords.Where(y => y.EntityName == x.EntityName && y.EntityId == x.EntityId).Count()
                };

            var result = query
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            return result;
        }

        public IDistributedLock GetLock<T>(T entity, string seName, string displayName, bool ensureNotEmpty, out string lockKey) where T : ISlugSupported
        {
            Guard.NotNull(entity);

            lockKey = seName.NullEmpty() ?? displayName;

            if (ensureNotEmpty && string.IsNullOrEmpty(lockKey))
            {
                // Use entity identifier as key if empty
                lockKey = entity.GetEntityName().ToLower() + entity.Id.ToStringInvariant();
            }

            if (string.IsNullOrEmpty(lockKey))
            {
                return null;
            }

            return DistributedSemaphoreLockProvider.Instance.GetLock("slug:" + lockKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal protected static bool FoundRecordIsSelf(ISlugSupported source, UrlRecord urlRecord, int? languageId)
        {
            return urlRecord != null
                && urlRecord.EntityId == source.Id
                && urlRecord.EntityName.EqualsNoCase(source.GetEntityName())
                && (languageId == null || urlRecord.LanguageId == languageId.Value);
        }

        #endregion
    }
}
