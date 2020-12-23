using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Catalog.Products
{
    public class ProductHook : AsyncDbSaveHook<BaseEntity>
    {
        private readonly SmartDbContext _db;

        public ProductHook(SmartDbContext db)
        {
            _db = db;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Products.
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

            // ProductMediaFile.
            var deletedProductFiles = entries
                .Where(x => x.State == Smartstore.Data.EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<ProductMediaFile>()
                .ToList();

            // Unassign deleted pictures from variant combinations.
            if (deletedProductFiles.Any())
            {
                var deletedMediaIds = deletedProductFiles.ToMultimap(x => x.ProductId, x => x.MediaFileId);
                var productIds = deletedProductFiles.Select(x => x.ProductId).Distinct().ToArray();

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