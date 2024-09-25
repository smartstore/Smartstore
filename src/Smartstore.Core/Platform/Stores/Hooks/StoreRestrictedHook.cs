using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Stores
{
    internal class StoreRestrictedHook : AsyncDbSaveHook<IStoreRestricted>
    {
        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;

        public StoreRestrictedHook(SmartDbContext db, ICacheManager cache)
        {
            _db = db;
            _cache = cache;
        }

        protected override Task<HookResult> OnDeletedAsync(IStoreRestricted entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .ToList();

            if (deletedEntities.Count > 0)
            {
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    var numDeleted = await _db.StoreMappings
                        .Where(x => entityIds.Contains(x.EntityId) && x.EntityName == entityName)
                        .ExecuteDeleteAsync(cancelToken);
                    if (numDeleted > 0)
                    {
                        await _cache.RemoveByPatternAsync(StoreMappingService.STOREMAPPING_SEGMENT_PATTERN);
                    }
                }
            }
        }
    }
}
