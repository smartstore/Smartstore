﻿using AngleSharp.Dom;
using Smartstore.Core.Catalog.Products.Utilities;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    internal class ProductMediaFileHook(SmartDbContext db) : AsyncDbSaveHook<ProductMediaFile>
    {
        private readonly SmartDbContext _db = db;

        protected override Task<HookResult> OnInsertedAsync(ProductMediaFile entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override async Task<HookResult> OnUpdatingAsync(ProductMediaFile entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            await _db.LoadReferenceAsync(entity, x => x.Product, false, q => q.Include(x => x.ProductMediaFiles), cancelToken);
            ProductPictureHelper.FixProductMainPictureId(_db, entity.Product);

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(ProductMediaFile entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var addedMediaFiles = entries
                .Where(x => x.InitialState == EntityState.Added)
                .Select(x => x.Entity)
                .OfType<ProductMediaFile>()
                .ToList();

            if (addedMediaFiles.Count > 0)
            {
                var productIds = addedMediaFiles.ToDistinctArray(x => x.ProductId);
                foreach (var productIdsChunk in productIds.Chunk(100))
                {
                    await FixMainPictureId(productIdsChunk, cancelToken);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }

            var deletedMediaFiles = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductMediaFile>()
                .ToList();

            if (deletedMediaFiles.Count > 0)
            {
                var deletedMediaIds = deletedMediaFiles.ToMultimap(x => x.ProductId, x => x.MediaFileId);
                var productIds = deletedMediaFiles.ToDistinctArray(x => x.ProductId);

                // Process the products in batches as they can have a large number of variant combinations assigned to them.
                foreach (var productIdsChunk in productIds.Chunk(100))
                {
                    await FixMainPictureId(productIdsChunk, cancelToken);

                    // Unassign deleted pictures from variant combinations.
                    var combinations = await _db.ProductVariantAttributeCombinations
                        .Where(x => productIdsChunk.Contains(x.ProductId) && !string.IsNullOrEmpty(x.AssignedMediaFileIds))
                        .ToListAsync(cancelToken);

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

                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }

        private async Task FixMainPictureId(IEnumerable<int> productIds, CancellationToken cancelToken)
        {
            var products = await _db.Products
                .Include(x => x.ProductMediaFiles)
                .Where(x => productIds.Contains(x.Id))
                .ToListAsync(cancelToken);

            foreach (var product in products)
            {
                if (product.ProductMediaFiles.Count == 0)
                {
                    product.MainPictureId = null;
                }
                else
                {
                    ProductPictureHelper.FixProductMainPictureId(_db, product);
                }
            }
        }
    }
}
