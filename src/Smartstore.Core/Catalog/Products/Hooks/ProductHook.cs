using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    [Important]
    internal class ProductHook : AsyncDbSaveHook<Product>
    {
        private readonly SmartDbContext _db;

        public ProductHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnInsertedAsync(Product entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        // We are overriding OnUpdatedAsync because SoftDeletableHook is pre-processing the entity and updating its entity state.
        protected override Task<HookResult> OnUpdatedAsync(Product entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var softDeletedProducts = entries
                .Where(x => x.IsSoftDeleted == true)
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            if (softDeletedProducts.Any())
            {
                foreach (var softDeletedProduct in softDeletedProducts)
                {
                    softDeletedProduct.Deleted = true;
                    softDeletedProduct.DeliveryTimeId = null;
                    softDeletedProduct.QuantityUnitId = null;
                    softDeletedProduct.CountryOfOriginId = null;
                    softDeletedProduct.ComparePriceLabelId = null;
                }

                await _db.SaveChangesAsync(cancelToken);

                // Unassign grouped products.
                var groupedProductIds = softDeletedProducts
                    .Where(x => x.ProductType == ProductType.GroupedProduct)
                    .Select(x => x.Id)
                    .Distinct()
                    .ToArray();

                if (groupedProductIds.Any())
                {
                    var allAssociatedProducts = await _db.Products
                        .Where(x => groupedProductIds.Contains(x.ParentGroupedProductId))
                        .ExecuteUpdateAsync(
                            x => x.SetProperty(p => p.ParentGroupedProductId, p => 0));
                }
            }

            // Update HasDiscountsApplied property.
            var products = entries
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            // Process in batches to avoid errors due to too long SQL statements.
            foreach (var productsChunk in products.Chunk(100))
            {
                var productIdsChunk = productsChunk
                    .Select(x => x.Id)
                    .ToArray();

                var appliedProductIds = await _db.Discounts
                    .SelectMany(x => x.AppliedToProducts)
                    .Where(x => productIdsChunk.Contains(x.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .ToListAsync(cancelToken);

                productsChunk.Each(x => x.HasDiscountsApplied = appliedProductIds.Contains(x.Id));
            }

            await _db.SaveChangesAsync(cancelToken);
        }
    }
}