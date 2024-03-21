using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    internal class EntityWithAttributesHook(SmartDbContext db) : AsyncDbSaveHook<EntityWithAttributes>
    {
        private readonly SmartDbContext _db = db;

        protected override Task<HookResult> OnDeletedAsync(EntityWithAttributes entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<EntityWithAttributes>()
                .Select(x => x as BaseEntity)
                .ToList();

            if (deletedEntities.Count > 0)
            {
                // Delete associated generic attributes.
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    foreach (var chunk in entityIds.Chunk(500))
                    {
                        await _db.GenericAttributes
                            .Where(x => chunk.Contains(x.EntityId) && x.KeyGroup == entityName)
                            .ExecuteDeleteAsync(cancelToken);
                    }
                }
            }
        }
    }
}
