using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Security
{
    internal class AclRestrictedHook : AsyncDbSaveHook<IAclRestricted>
    {
        private readonly SmartDbContext _db;

        public AclRestrictedHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(IAclRestricted entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<IAclRestricted>()
                .Select(x => x as BaseEntity)
                .ToList();

            if (deletedEntities.Any())
            {
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    await _db.AclRecords
                        .Where(x => entityIds.Contains(x.EntityId) && x.EntityName == entityName)
                        .ExecuteDeleteAsync(cancelToken);
                }
            }
        }
    }
}
