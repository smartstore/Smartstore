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
    [Important]
    public class ProductHook : AsyncDbSaveHook<Product>
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
                }

                await _db.SaveChangesAsync();

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
                        .BatchUpdateAsync(x => new Product { ParentGroupedProductId = 0 });
                }
            }

            // Update HasDiscountsApplied property.
            var products = entries
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            // TODO: (mg) (core) (PERF) It's not certain that "AppliedDiscounts" has been eager loaded. If not, this will slow down things like shit!
            products.Each(x => x.HasDiscountsApplied = x.AppliedDiscounts.Any());

            await _db.SaveChangesAsync();
        }
    }
}