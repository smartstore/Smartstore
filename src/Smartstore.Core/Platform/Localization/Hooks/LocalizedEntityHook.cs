using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Localization
{
    internal class LocalizedEntityHook : AsyncDbSaveHook<ILocalizedEntity>
    {
        private readonly SmartDbContext _db;

        public LocalizedEntityHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(ILocalizedEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ILocalizedEntity>()
                .Select(x => x as BaseEntity)
                .ToList();

            if (deletedEntities.Any())
            {
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    await _db.LocalizedProperties
                        .Where(x => entityIds.Contains(x.EntityId) && x.LocaleKeyGroup == entityName)
                        .ExecuteDeleteAsync(cancelToken);
                }
            }
        }
    }
}
