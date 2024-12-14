using System.Runtime.CompilerServices;
using System.Text;
using Smartstore.Caching;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    public partial class LocalizedEntityService : AsyncDbSaveHook<LocalizedProperty>, ILocalizedEntityService
    {
        /// <summary>
        /// 0 = segment (keygroup.key.idrange), 1 = language id
        /// </summary>
        private readonly static CompositeFormat LOCALIZEDPROPERTY_SEGMENT_KEY = CompositeFormat.Parse("localizedproperty:{0}-lang-{1}");
        const string LOCALIZEDPROPERTY_SEGMENT_PATTERN = "localizedproperty:{0}*";
        const string LOCALIZEDPROPERTY_ALLSEGMENTS_PATTERN = "localizedproperty:*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly PerformanceSettings _performanceSettings;

        private readonly IDictionary<string, LocalizedPropertyCollection> _prefetchedCollections;
        private static int _lastCacheSegmentSize = -1;

        public LocalizedEntityService(SmartDbContext db, ICacheManager cache, PerformanceSettings performanceSettings)
        {
            _db = db;
            _cache = cache;
            _performanceSettings = performanceSettings;

            _prefetchedCollections = new Dictionary<string, LocalizedPropertyCollection>(StringComparer.OrdinalIgnoreCase);

            ValidateCacheState();
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
                _cache.RemoveByPattern(LOCALIZEDPROPERTY_ALLSEGMENTS_PATTERN);
                changed = true;
            }

            if (changed)
            {
                Interlocked.Exchange(ref _lastCacheSegmentSize, size);
            }
        }

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var tasks = entries
                .Select(x => x.Entity)
                .OfType<LocalizedProperty>()
                .Where(x => !x.IsHidden)
                .Select(x => GetSegmentKeyPart(x.LocaleKeyGroup, x.LocaleKey, x.EntityId))
                .Distinct()
                .Select(x => _cache.RemoveByPatternAsync(LOCALIZEDPROPERTY_SEGMENT_PATTERN.FormatInvariant(x)))
                .ToArray();

            return Task.WhenAll(tasks);
        }

        #endregion

        #region ILocalizedEntityService

        public virtual string GetLocalizedValue(int languageId, int entityId, string localeKeyGroup, string localeKey)
        {
            if (TryGetPrefetched(languageId, entityId, localeKeyGroup, localeKey, out var localeValue))
            {
                return localeValue;
            }

            if (languageId <= 0)
            {
                return string.Empty;
            } 

            var props = GetCacheSegment(localeKeyGroup, localeKey, entityId, languageId);
            if (!props.TryGetValue(entityId, out var val))
            {
                return string.Empty;
            }

            return val;
        }

        public virtual async Task<string> GetLocalizedValueAsync(int languageId, int entityId, string localeKeyGroup, string localeKey)
        {
            if (TryGetPrefetched(languageId, entityId, localeKeyGroup, localeKey, out var localeValue))
            {
                return localeValue;
            }

            if (languageId <= 0)
            {
                return string.Empty;
            }

            var props = await GetCacheSegmentAsync(localeKeyGroup, localeKey, entityId, languageId);
            if (!props.TryGetValue(entityId, out var val))
            {
                return string.Empty;
            }

            return val;
        }

        private bool TryGetPrefetched(int languageId, int entityId, string localeKeyGroup, string localeKey, out string localeValue)
        {
            localeValue = null;

            if (_prefetchedCollections.TryGetValue(localeKeyGroup, out var collection))
            {
                var cachedItem = collection.Find(languageId, entityId, localeKey);
                if (cachedItem != null)
                {
                    localeValue = cachedItem.LocaleValue;
                    return true;
                }
            }

            return false;
        }

        public virtual async Task PrefetchLocalizedPropertiesAsync(
            string localeKeyGroup, 
            int languageId,
            int[] entityIds,
            bool isRange = false, 
            bool isSorted = false)
        {
            if (languageId == 0)
                return;

            var collection = await GetLocalizedPropertyCollectionInternal(localeKeyGroup, languageId, entityIds, isRange, isSorted);

            if (_prefetchedCollections.TryGetValue(localeKeyGroup, out var existing))
            {
                collection.MergeWith(existing);
            }
            else
            {
                _prefetchedCollections[localeKeyGroup] = collection;
            }
        }

        public virtual Task<LocalizedPropertyCollection> GetLocalizedPropertyCollectionAsync(
            string localeKeyGroup, 
            int[] entityIds, 
            bool isRange = false, 
            bool isSorted = false)
        {
            return GetLocalizedPropertyCollectionInternal(localeKeyGroup, 0, entityIds, isRange, isSorted);
        }

        protected virtual async Task<LocalizedPropertyCollection> GetLocalizedPropertyCollectionInternal(
            string localeKeyGroup,
            int languageId,
            int[] entityIds,
            bool isRange = false,
            bool isSorted = false)
        {
            Guard.NotEmpty(localeKeyGroup);

            using (new DbContextScope(_db, lazyLoading: false))
            {
                var query = from x in _db.LocalizedProperties.AsNoTracking()
                            where x.LocaleKeyGroup == localeKeyGroup
                            select x;

                var splitEntityIds = false;
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
                        var max = entityIds[^1];

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

                        if (entityIds.Length > 5000)
                        {
                            splitEntityIds = true;
                        }
                        else
                        {
                            query = query.Where(x => entityIds.Contains(x.EntityId));
                        }
                    }
                }

                if (languageId > 0)
                {
                    query = query.Where(x => x.LanguageId == languageId);
                }

                // (perf) Should come last, because "IsHidden" has no index
                query = query.Where(x => !x.IsHidden);

                if (splitEntityIds)
                {
                    var items = new List<LocalizedProperty>();
                    foreach (var chunk in entityIds.Chunk(5000))
                    {
                        items.AddRange(await query.Where(x => chunk.Contains(x.EntityId)).ToListAsync());
                    }

                    return new LocalizedPropertyCollection(localeKeyGroup, requestedSet, items);
                }
                else
                {
                    return new LocalizedPropertyCollection(localeKeyGroup, requestedSet, await query.ToListAsync());
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task<LocalizedProperty> ApplyLocalizedValueAsync<T>(
            T entity,
            Expression<Func<T, string>> keySelector,
            string value,
            int languageId) where T : class, ILocalizedEntity
        {
            return ApplyLocalizedValueAsync(entity, entity.Id, entity.GetEntityName(), keySelector, value, languageId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task<LocalizedProperty> ApplyLocalizedValueAsync<T, TPropType>(
            T entity,
            Expression<Func<T, TPropType>> keySelector,
            TPropType value,
            int languageId) where T : class, ILocalizedEntity
        {
            return ApplyLocalizedValueAsync(entity, entity.Id, entity.GetEntityName(), keySelector, value, languageId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Task<LocalizedProperty> ApplyLocalizedSettingAsync<TSetting, TPropType>(
            TSetting settings,
            Expression<Func<TSetting, TPropType>> keySelector,
            TPropType value,
            int languageId,
            int storeId = 0) where TSetting : class, ISettings
        {
            // INFO: unfortunately we have to misuse the "EntityId" prop and store StoreId instead.
            return ApplyLocalizedValueAsync(settings, storeId, typeof(TSetting).Name, keySelector, value, languageId);
        }

        public virtual LocalizedProperty ApplyLocalizedValue(
            LocalizedProperty localizedProperty,
            int entityId,
            string localeKeyGroup,
            string localeKey,
            object value,
            int languageId)
        {
            Guard.NotEmpty(localeKeyGroup);
            Guard.NotZero(languageId);

            var valueStr = value?.Convert<string>();

            if (localizedProperty != null)
            {
                if (string.IsNullOrEmpty(valueStr))
                {
                    if (!localizedProperty.IsHidden)
                    {
                        // Delete (but only visible/user-defined entries)
                        _db.LocalizedProperties.Remove(localizedProperty);
                    }
                }
                else
                {
                    // Update
                    if (localizedProperty.LocaleValue != valueStr)
                    {
                        localizedProperty.LocaleValue = valueStr;

                        // User modified entry, so this cannot be hidden anymore.
                        localizedProperty.IsHidden = false;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(valueStr))
                {
                    // Insert
                    localizedProperty = new LocalizedProperty
                    {
                        EntityId = entityId,
                        LanguageId = languageId,
                        LocaleKey = localeKey,
                        LocaleKeyGroup = localeKeyGroup,
                        LocaleValue = valueStr
                    };
                    _db.LocalizedProperties.Add(localizedProperty);
                }
            }

            return localizedProperty;
        }

        protected virtual async Task<LocalizedProperty> ApplyLocalizedValueAsync<T, TPropType>(
            T obj,
            int id, // T is BaseEntity = EntityId, T is ISetting = StoreId
            string keyGroup,
            Expression<Func<T, TPropType>> keySelector,
            TPropType value,
            int languageId) where T : class
        {
            Guard.NotNull(obj);

            var propInfo = keySelector.ExtractPropertyInfo() ?? throw new ArgumentException($"Expression '{keySelector}' does not refer to a property.");

            var entity = await _db.LocalizedProperties
                .ApplyStandardFilter(languageId, id, keyGroup, propInfo.Name)
                .FirstOrDefaultAsync();

            return ApplyLocalizedValue(entity, id, keyGroup, propInfo.Name, value, languageId);
        }

        public virtual Task ClearCacheAsync()
            => _cache.RemoveByPatternAsync(LOCALIZEDPROPERTY_ALLSEGMENTS_PATTERN);

        #endregion

        #region Cache segments

        protected virtual Dictionary<int, string> GetCacheSegment(string localeKeyGroup, string localeKey, int entityId, int languageId)
        {
            Guard.NotEmpty(localeKeyGroup);
            Guard.NotEmpty(localeKey);

            var segmentKey = GetSegmentKeyPart(localeKeyGroup, localeKey, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey, languageId);

            // TODO: (MC) skip caching product.fulldescription (?), OR
            // ...additionally segment by entity id ranges.

            return _cache.Get(cacheKey, () =>
            {
                var properties = _db.LocalizedProperties
                    .AsNoTracking()
                    .Where(x => x.EntityId >= minEntityId 
                        && x.EntityId <= maxEntityId 
                        && x.LocaleKey == localeKey 
                        && x.LocaleKeyGroup == localeKeyGroup 
                        && x.LanguageId == languageId
                        && !x.IsHidden)
                    .ToList();

                var dict = new Dictionary<int, string>(properties.Count);

                foreach (var prop in properties)
                {
                    dict[prop.EntityId] = prop.LocaleValue ?? string.Empty;
                }

                return dict;
            });
        }

        protected virtual Task<Dictionary<int, string>> GetCacheSegmentAsync(string localeKeyGroup, string localeKey, int entityId, int languageId)
        {
            Guard.NotEmpty(localeKeyGroup);
            Guard.NotEmpty(localeKey);

            var segmentKey = GetSegmentKeyPart(localeKeyGroup, localeKey, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey, languageId);

            // TODO: (MC) skip caching product.fulldescription (?), OR
            // ...additionally segment by entity id ranges.

            return _cache.GetAsync(cacheKey, async () =>
            {
                var properties = await _db.LocalizedProperties
                    .AsNoTracking()
                    .Where(x => x.EntityId >= minEntityId 
                        && x.EntityId <= maxEntityId 
                        && x.LocaleKey == localeKey 
                        && x.LocaleKeyGroup == localeKeyGroup 
                        && x.LanguageId == languageId
                        && !x.IsHidden)
                    .ToListAsync();

                var dict = new Dictionary<int, string>(properties.Count);

                foreach (var prop in properties)
                {
                    dict[prop.EntityId] = prop.LocaleValue ?? string.Empty;
                }

                return dict;
            });
        }

        /// <summary>
        /// Clears the cached segment from the cache
        /// </summary>
        protected virtual Task ClearCacheSegmentAsync(string localeKeyGroup, string localeKey, int entityId, int? languageId = null)
        {
            var segmentKey = GetSegmentKeyPart(localeKeyGroup, localeKey, entityId);

            if (languageId.HasValue && languageId.Value > 0)
            {
                return _cache.RemoveAsync(BuildCacheSegmentKey(segmentKey, languageId.Value));
            }
            else
            {
                return _cache.RemoveByPatternAsync(LOCALIZEDPROPERTY_SEGMENT_PATTERN.FormatInvariant(segmentKey));
            }
        }

        private static string BuildCacheSegmentKey(string segment, int languageId)
        {
            return LOCALIZEDPROPERTY_SEGMENT_KEY.FormatInvariant(segment, languageId);
        }

        private string GetSegmentKeyPart(string localeKeyGroup, string localeKey, int entityId)
        {
            return GetSegmentKeyPart(localeKeyGroup, localeKey, entityId, out _, out _);
        }

        private string GetSegmentKeyPart(string localeKeyGroup, string localeKey, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(_performanceSettings.CacheSegmentSize);
            return (localeKeyGroup + "." + localeKey + "." + minId.ToString()).ToLowerInvariant();
        }

        #endregion
    }
}
