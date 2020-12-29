using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductBundleItemHook : AsyncDbSaveHook<ProductBundleItem>
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
                    .ToListAsync();

                foreach (var parentItemId in parentItemIds)
                {
                    var childItemIds = await _db.ShoppingCartItems
                        .Where(x => x.ParentItemId != null && x.ParentItemId == parentItemId && x.Id != parentItemId)
                        .Select(x => x.Id)
                        .ToListAsync();

                    await _db.ShoppingCartItems
                        .Where(x => childItemIds.Contains(x.Id))
                        .BatchDeleteAsync();
                }

                await _db.ShoppingCartItems
                    .Where(x => parentItemIds.Contains(x.Id))
                    .BatchDeleteAsync();
            }
        }
    }
}
