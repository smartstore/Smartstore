using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

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

        public StoreMappingService(ICacheManager cache, IStoreContext storeContext, SmartDbContext db)
        {
            _cache = cache;
            _storeContext = storeContext;
            _db = db;
        }

        public DbQuerySettings QuerySettings { get; set; } = DbQuerySettings.Default;

        #region Hook

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var entity = entry.Entity as StoreMapping;
            await ClearCacheSegmentAsync(entity.EntityName, entity.EntityId);
            return HookResult.Ok;
        }

        #endregion

        public virtual async Task ApplyStoreMappingsAsync<T>(T entity, int[] selectedStoreIds) 
            where T : BaseEntity, IStoreRestricted
        {
            var existingStoreMappings = await _db.StoreMappings
                .ApplyEntityFilter(entity)
                .ToListAsync();
            
            var allStores = _storeContext.GetAllStores();
            selectedStoreIds ??= Array.Empty<int>();

            entity.LimitedToStores = (selectedStoreIds.Length != 1 || selectedStoreIds[0] != 0) && selectedStoreIds.Any();

            foreach (var store in allStores)
            {
                if (selectedStoreIds.Contains(store.Id))
                {
                    if (!existingStoreMappings.Any(x => x.StoreId == store.Id))
                    {
                        AddStoreMapping(entity, store.Id);
                    }  
                }
                else
                {
                    var storeMappingToDelete = existingStoreMappings.FirstOrDefault(x => x.StoreId == store.Id);
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

            if (storeId == 0)
                throw new ArgumentOutOfRangeException(nameof(storeId));

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

        private string GetSegmentKeyPart(string entityName, int entityId)
        {
            return GetSegmentKeyPart(entityName, entityId, out _, out _);
        }

        private string GetSegmentKeyPart(string entityName, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(1000);
            return (entityName + "." + minId.ToString()).ToLowerInvariant();
        }

        #endregion
    }
}