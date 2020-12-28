using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    public class ProductVariantAttributeCombinationHook : AsyncDbSaveHook<ProductVariantAttributeCombination>
    {
        private readonly SmartDbContext _db;

        public ProductVariantAttributeCombinationHook(SmartDbContext db)
        {
            _db = db;
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken) => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var variantCombinations = entries
                .Select(x => x.Entity)
                .OfType<ProductVariantAttributeCombination>()
                .ToList();

            // Update product property for the lowest attribute combination price.
            if (variantCombinations.Any())
            {
                var productIds = variantCombinations
                    .Select(x => x.ProductId)
                    .Distinct()
                    .ToArray();

                foreach (var productIdsChunk in productIds.Slice(100))
                {
                    var variantCombinationQuery =
                        from pvac in _db.ProductVariantAttributeCombinations
                        where productIdsChunk.Contains(pvac.ProductId) && pvac.Price != null && pvac.IsActive
                        select pvac;

                    var lowestPricesQuery =
                        from x in variantCombinationQuery
                        group x by x.ProductId into grp
                        select new
                        {
                            ProductId = grp.Key,
                            LowestPrice = grp
                                .OrderBy(y => y.Price)
                                .Select(y => y.Price)
                                .FirstOrDefault()
                        };

                    var lowestPrices = await lowestPricesQuery.ToListAsync();
                    var lowestPricesDic = lowestPrices.ToDictionarySafe(x => x.ProductId, x => x.LowestPrice);

                    var products = await _db.Products
                        .Where(x => productIdsChunk.Contains(x.Id))
                        .ToListAsync();

                    foreach (var product in products)
                    {                   
                        product.LowestAttributeCombinationPrice = lowestPricesDic.GetValueOrDefault(product.Id);
                    }

                    await _db.SaveChangesAsync();
                }
            }
        }
    }
}
