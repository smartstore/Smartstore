using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductMediaFileHook : AsyncDbSaveHook<ProductMediaFile>
    {
        private readonly SmartDbContext _db;

        public ProductMediaFileHook(SmartDbContext db)
        {
            _db = db;
        }

        // We must return HookResult.Ok otherwise DefaultDbHookHandler.SavedChangesAsync does not call OnAfterSaveCompletedAsync.
        protected override Task<HookResult> OnDeletedAsync(ProductMediaFile entity, IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedMediaFiles = entries
                .Where(x => x.InitialState == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductMediaFile>()
                .ToList();

            // Unassign deleted pictures from variant combinations.
            if (deletedMediaFiles.Any())
            {
                var deletedMediaIds = deletedMediaFiles.ToMultimap(x => x.ProductId, x => x.MediaFileId);
                var productIds = deletedMediaFiles.Select(x => x.ProductId).Distinct().ToArray();

                foreach (var productIdsChunk in productIds.Slice(100))
                {
                    var combinations = await _db.ProductVariantAttributeCombinations
                        .Where(x => productIdsChunk.Contains(x.ProductId) && !string.IsNullOrEmpty(x.AssignedMediaFileIds))
                        .ToListAsync();

                    foreach (var combination in combinations)
                    {
                        if (deletedMediaIds.ContainsKey(combination.ProductId))
                        {
                            var newMediaIds = combination
                                .GetAssignedMediaIds()
                                .Except(deletedMediaIds[combination.ProductId])
                                .ToArray();

                            combination.SetAssignedMediaIds(newMediaIds);
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}
