using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Stores
{
    [Important]
    public partial class StoreMappingService : AsyncDbSaveHook<StoreMapping>, IStoreMappingService
    {
        /// <summary>
        /// 0 = segment (EntityName.IdRange)
        /// </summary>
        const string STOREMAPPING_SEGMENT_KEY = "storemapping:range-{0}";
        internal const string STOREMAPPING_SEGMENT_PATTERN = "storemapping:range-*";

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cache;
        private readonly IDictionary<string, StoreMappingCollection> _prefetchedCollections;

        public StoreMappingService(ICacheManager cache, IStoreContext storeContext, SmartDbContext db)
        {
            _cache = cache;
            _storeContext = storeContext;
            _db = db;

            _prefetchedCollections = new Dictionary<string, StoreMappingCollection>(StringComparer.OrdinalIgnoreCase);
        }

        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var distinctEntries = entries
                .Select(x => x.Entity)
                .OfType<StoreMapping>()
                .Select(x => (x.EntityName, x.EntityId))
                .Distinct()
                .ToArray();

            foreach (var entry in distinctEntries)
            {
                await ClearCacheSegmentAsync(entry.EntityName, entry.EntityId);
            }
        }

        #endregion

        public virtual async Task ApplyStoreMappingsAsync<T>(T entity, int[] selectedStoreIds)
            where T : BaseEntity, IStoreRestricted
        {
            selectedStoreIds ??= Array.Empty<int>();

            List<StoreMapping> lookup = null;
            var allStores = _storeContext.GetAllStores();

            entity.LimitedToStores = (selectedStoreIds.Length != 1 || selectedStoreIds[0] != 0) && selectedStoreIds.Any();

            foreach (var store in allStores)
            {
                if (selectedStoreIds.Contains(store.Id))
                {
                    // Add the mapping, if missing.
                    if (await FindMapping(entity, store.Id, lookup) == null)
                    {
                        AddStoreMapping(entity, store.Id);
                    }
                }
                else
                {
                    // Delete the mapping, if it exists.
                    var storeMappingToDelete = await FindMapping(entity, store.Id, lookup);
                    if (storeMappingToDelete != null)
                    {
                        _db.StoreMappings.Remove(storeMappingToDelete);
                    }
                }
            }
        }

        public virtual void AddStoreMapping<T>(T entity, int storeId) where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotZero(storeId, nameof(storeId));

            _db.StoreMappings.Add(new StoreMapping
            {
                EntityId = entity.Id,
                EntityName = entity.GetEntityName(),
                StoreId = storeId
            });
        }

        public Task<bool> AuthorizeAsync(string entityName, int entityId)
        {
            return AuthorizeAsync(entityName, entityId, _storeContext.CurrentStore.Id);
        }

        public virtual async Task<bool> AuthorizeAsync(string entityName, int entityId, int storeId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            if (entityId <= 0)
                return false;

            if (storeId <= 0 || QuerySettings.IgnoreMultiStore)
                // return true if no store specified/found
                return true;


            // Permission granted only when the id list contains the passed storeId
            return (await GetAuthorizedStoreIdsAsync(entityName, entityId)).Any(x => x == storeId);
        }

        public virtual async Task<int[]> GetAuthorizedStoreIdsAsync(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            if (entityId <= 0)
                return Array.Empty<int>();

            var cacheSegment = await GetCacheSegmentAsync(entityName, entityId);

            if (!cacheSegment.TryGetValue(entityId, out var storeIds))
            {
                return Array.Empty<int>();
            }

            return storeIds;
        }

        public virtual async Task PrefetchStoreMappingsAsync(string entityName, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            var collection = await GetStoreMappingCollectionAsync(entityName, entityIds, isRange, isSorted, tracked);

            if (_prefetchedCollections.TryGetValue(entityName, out var existing))
            {
                collection.MergeWith(existing);
            }
            else
            {
                _prefetchedCollections[entityName] = collection;
            }
        }

        public virtual async Task<StoreMappingCollection> GetStoreMappingCollectionAsync(string entityName, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            var query = _db.StoreMappings
                .ApplyTracking(tracked)
                .Where(x => x.EntityName == entityName);

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
                    query = query.Where(x => entityIds.Contains(x.EntityId));
                }
            }

            var items = await query.OrderBy(x => x.Id).ToListAsync();

            return new StoreMappingCollection(entityName, requestedSet, items);
        }

        private async Task<StoreMapping> FindMapping<T>(T entity, int storeId, List<StoreMapping> lookup)
            where T : BaseEntity, IStoreRestricted
        {
            if (_prefetchedCollections.TryGetValue(entity.GetEntityName(), out var collection))
            {
                return collection.Find(entity.Id, storeId);
            }

            if (lookup == null)
            {
                lookup = await _db.StoreMappings
                    .ApplyEntityFilter(entity)
                    .ToListAsync();
            }

            return lookup.FirstOrDefault(x => x.StoreId == storeId);
        }

        #region Cache segmenting

        protected virtual Task<Dictionary<int, int[]>> GetCacheSegmentAsync(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            var segmentKey = GetSegmentKeyPart(entityName, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey);

            return _cache.GetAsync(cacheKey, async () =>
            {
                var query = from x in _db.StoreMappings.AsNoTracking()
                            where
                                x.EntityId >= minEntityId &&
                                x.EntityId <= maxEntityId &&
                                x.EntityName == entityName
                            select x;

                var mappings = (await query.ToListAsync()).ToLookup(x => x.EntityId, x => x.StoreId);

                var dict = new Dictionary<int, int[]>(mappings.Count);

                foreach (var mapping in mappings)
                {
                    dict[mapping.Key] = mapping.ToArray();
                }

                return dict;
            });
        }

        /// <summary>
        /// Clears the cached segment from the cache
        /// </summary>
        protected virtual Task ClearCacheSegmentAsync(string entityName, int entityId)
        {
            try
            {
                var segmentKey = GetSegmentKeyPart(entityName, entityId);
                return _cache.RemoveAsync(BuildCacheSegmentKey(segmentKey));
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        private static string BuildCacheSegmentKey(string segment)
        {
            return string.Format(STOREMAPPING_SEGMENT_KEY, segment);
        }

        private static string GetSegmentKeyPart(string entityName, int entityId)
        {
            return GetSegmentKeyPart(entityName, entityId, out _, out _);
        }

        private static string GetSegmentKeyPart(string entityName, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(1000);
            return (entityName + "." + minId.ToString()).ToLowerInvariant();
        }

        #endregion
    }
}