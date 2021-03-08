using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
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
            var deletedEntries = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted && x.Entity is ILocalizedEntity)
                .ToList();

            if (deletedEntries.Any())
            {
                foreach (var group in deletedEntries.GroupBy(x => x.EntityType.Name))
                {
                    var entityIds = group.Select(x => x.Entity.Id).ToArray();
                    var entityName = group.Key;

                    await _db.LocalizedProperties
                        .Where(x => entityIds.Contains(x.EntityId) && x.LocaleKeyGroup == entityName)
                        .BatchDeleteAsync(cancelToken);
                }
            }
        }
    }
}
