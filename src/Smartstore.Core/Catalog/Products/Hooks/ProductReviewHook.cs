using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    [Important]
    internal class ProductReviewHook : AsyncDbSaveHook<ProductReview>
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IProductService> _productService;
        private int[] _productIdsToUpdate;

        public ProductReviewHook(SmartDbContext db, Lazy<IProductService> productService)
        {
            _db = db;
            _productService = productService;
        }

        protected override Task<HookResult> OnDeletingAsync(ProductReview entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(ProductReview entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedReviews = entries
                .Where(x => x.State == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductReview>()
                .ToArray();
            if (deletedReviews.Length > 0)
            {
                _productIdsToUpdate = deletedReviews.ToDistinctArray(x => x.ProductId);

                // Delete review helpfulness entries because otherwise product reviews cannot be deleted.
                // INFO: Batch delete is not supported here because of TPT (Table-per-Type) inheritance.
                var deletedReviewIds = deletedReviews.ToDistinctArray(x => x.Id);
                var toDelete = await _db.ProductReviewHelpfulness
                    .Where(x => deletedReviewIds.Contains(x.ProductReviewId))
                    .ToListAsync(cancelToken);
                if (toDelete.Count > 0)
                {
                    _db.ProductReviewHelpfulness.RemoveRange(toDelete);
                    await _db.SaveChangesAsync(cancelToken);
                }
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (!_productIdsToUpdate.IsNullOrEmpty())
            {
                foreach (var productIdsChunk in _productIdsToUpdate.Chunk(100))
                {
                    // Avoid loading tracked entities (too slow) and apply stats on stub objects.
                    var productStubs = productIdsChunk
                        .Select(id => new Product { Id = id })
                        .ToList();
                    await _productService.Value.ApplyProductReviewTotalsAsync(productStubs, cancelToken);

                    foreach (var stub in productStubs)
                    {
                        await _db.Products
                            .Where(x => x.Id == stub.Id)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(x => x.ApprovedRatingSum, stub.ApprovedRatingSum)
                                .SetProperty(x => x.NotApprovedRatingSum, stub.NotApprovedRatingSum)
                                .SetProperty(x => x.ApprovedTotalReviews, stub.ApprovedTotalReviews)
                                .SetProperty(x => x.NotApprovedTotalReviews, stub.NotApprovedTotalReviews),
                                cancelToken);
                    }
                }
            }
        }
    }
}
