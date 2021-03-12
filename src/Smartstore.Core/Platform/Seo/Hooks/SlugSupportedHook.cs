using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Seo
{
    internal class SlugSupportedHook : AsyncDbSaveHook<ISlugSupported>
    {
        private readonly SmartDbContext _db;

        public SlugSupportedHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletedAsync(ISlugSupported entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedEntities = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ISlugSupported>()
                .ToList();

            if (deletedEntities.Any())
            {
                foreach (var group in deletedEntities.GroupBy(x => x.GetEntityName()))
                {
                    var entityIds = group.Select(x => x.Id).ToArray();
                    var entityName = group.Key;

                    await _db.UrlRecords
                        .Where(x => entityIds.Contains(x.EntityId) && x.EntityName == entityName)
                        .BatchDeleteAsync(cancelToken);
                }
            }
        }
    }
}
