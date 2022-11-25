using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Stores
{
    internal class StoreRestrictedHook : AsyncDbSaveHook<IStoreRestricted>
    {
        private readonly SmartDbContext _db;

        public StoreRestrictedHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(IStoreRestricted entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .ToList();

            if (deletedEntities.Any())
            {
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    await _db.StoreMappings
                        .Where(x => entityIds.Contains(x.EntityId) && x.EntityName == entityName)
                        .ExecuteDeleteAsync(cancelToken);
                }
            }
        }
    }
}
