using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Stores
{
    internal class StoreHook : AsyncDbSaveHook<Store>
    {
        private readonly SmartDbContext _db;

        public StoreHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(Store entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedStoreIds = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<Store>()
                .Select(x => x.Id)
                .ToList();

            if (deletedStoreIds.Any())
            {
                // When we delete a store we should also ensure that all "per store" settings will also be deleted.
                await _db.Settings
                    .Where(x => deletedStoreIds.Contains(x.StoreId))
                    .ExecuteDeleteAsync(cancelToken);

                // When we had two stores and now have only one store, we also should delete all "per store" settings.
                var allStoreIds = await _db.Stores
                    .Select(x => x.Id)
                    .ToListAsync(cancelToken);

                if (allStoreIds.Count == 1)
                {
                    await _db.Settings
                        .Where(x => x.StoreId == allStoreIds[0])
                        .ExecuteDeleteAsync(cancelToken);
                }
            }
        }
    }
}
