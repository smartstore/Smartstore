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
                // TODO: (mg) (core) EF Core only supports server translation of GroupBy with key projection!! What? Really? Find solution (two queries?).
                // Process the products in batches as they can have a large number of variant combinations assigned to them.
                //foreach (var productIdsChunk in productIds.Slice(100))
                //{
                //    var variantCombinationQuery =
                //        from pvac in _db.ProductVariantAttributeCombinations
                //        where productIdsChunk.Contains(pvac.ProductId) && pvac.Price != null && pvac.IsActive
                //        select pvac;

                //    var lowestPricesQuery =
                //        from x in variantCombinationQuery
                //        group x by x.ProductId into grp
                //        select new
                //        {
                //            ProductId = grp.Key,
                //            LowestPrice = grp
                //                .OrderBy(y => y.Price)
                //                .Select(y => y.Price)
                //                .FirstOrDefault()
                //        };

                //    var lowestPrices = await lowestPricesQuery.ToListAsync();
                //    var lowestPricesDic = lowestPrices.ToDictionarySafe(x => x.ProductId, x => x.LowestPrice);

                //    foreach (var productId in productIdsChunk)
                //    {
                //        var lowestAttributeCombinationPrice = lowestPricesDic.GetValueOrDefault(productId);

                //        // BatchUpdate recommended because products contain a lot of data (like full description).
                //        await _db.Products
                //            .Where(x => x.Id == productId)
                //            .BatchUpdateAsync(x => new Product { LowestAttributeCombinationPrice = lowestAttributeCombinationPrice });
                //    }
                //}
            }
        }
    }
}
