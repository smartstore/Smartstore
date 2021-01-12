using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Batching;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    public class ProductVariantAttributeCombinationHook : AsyncDbSaveHook<ProductVariantAttributeCombination>
    {
        private readonly SmartDbContext _db;

        public ProductVariantAttributeCombinationHook(SmartDbContext db)
        {
            _db = db;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken) 
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var variantCombinations = entries
                .Select(x => x.Entity)
                .OfType<ProductVariantAttributeCombination>()
                .ToList();

            // Update product property for the lowest attribute combination price.
            var productIds = variantCombinations
                .Select(x => x.ProductId)
                .Distinct()
                .ToArray();

            if (productIds.Any())
            {
                // Process the products in batches as they can have a large number of variant combinations assigned to them.
                foreach (var productIdsChunk in productIds.Slice(100))
                {
                    var lowestPricesQuery =
                        from x in _db.ProductVariantAttributeCombinations
                        where productIdsChunk.Contains(x.ProductId) && x.Price != null && x.IsActive
                        group x by x.ProductId into grp
                        select new
                        {
                            ProductId = grp.Key,
                            LowestPrice = _db.ProductVariantAttributeCombinations
                                .Where(y => y.ProductId == grp.Key && y.Price != null && y.IsActive)
                                .OrderBy(y => y.Price)
                                .Select(y => y.Price)
                                .FirstOrDefault()
                        };

                    var lowestPrices = await lowestPricesQuery.ToListAsync();
                    var lowestPricesDic = lowestPrices.ToDictionarySafe(x => x.ProductId, x => x.LowestPrice);

                    foreach (var productId in productIdsChunk)
                    {
                        var lowestAttributeCombinationPrice = lowestPricesDic.GetValueOrDefault(productId);

                        // BatchUpdate recommended because products contain a lot of data (like full description).
                        await _db.Products
                            .Where(x => x.Id == productId)
                            .BatchUpdateAsync(x => new Product { LowestAttributeCombinationPrice = lowestAttributeCombinationPrice });
                    }
                }
            }
        }
    }
}
