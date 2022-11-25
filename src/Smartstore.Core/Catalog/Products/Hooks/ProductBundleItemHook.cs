using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    internal class ProductBundleItemHook : AsyncDbSaveHook<ProductBundleItem>
    {
        private readonly SmartDbContext _db;

        public ProductBundleItemHook(SmartDbContext db)
        {
            _db = db;
        }

        protected override Task<HookResult> OnDeletingAsync(ProductBundleItem entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedBundleItemIds = entries
                .Where(x => x.State == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductBundleItem>()
                .Select(x => x.Id)
                .ToList();

            if (deletedBundleItemIds.Any())
            {
                // Remove bundles from shopping carts because otherwise bundle items cannot be deleted.
                var parentItemIds = await _db.ShoppingCartItems
                    .Where(x => deletedBundleItemIds.Contains(x.BundleItemId ?? 0) && x.ParentItemId != null)
                    .Select(x => x.ParentItemId)
                    .Distinct()
                    .ToListAsync(cancelToken);

                foreach (var parentItemId in parentItemIds)
                {
                    var childItemIds = await _db.ShoppingCartItems
                        .Where(x => x.ParentItemId != null && x.ParentItemId == parentItemId && x.Id != parentItemId)
                        .Select(x => x.Id)
                        .ToListAsync(cancelToken);

                    await _db.ShoppingCartItems
                        .Where(x => childItemIds.Contains(x.Id))
                        .ExecuteDeleteAsync(cancelToken);
                }

                await _db.ShoppingCartItems
                    .Where(x => parentItemIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancelToken);
            }
        }
    }
}
