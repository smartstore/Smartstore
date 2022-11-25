using Smartstore.Caching;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Attributes
{
    [Important]
    internal class ProductVariantAttributeCombinationHook : AsyncDbSaveHook<ProductVariantAttributeCombination>
    {
        private readonly SmartDbContext _db;
        private readonly IRequestCache _requestCache;

        public ProductVariantAttributeCombinationHook(SmartDbContext db, IRequestCache requestCache)
        {
            _db = db;
            _requestCache = requestCache;
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
                foreach (var productIdsChunk in productIds.Chunk(100))
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

                    var lowestPrices = await lowestPricesQuery.ToListAsync(cancelToken);
                    var lowestPricesDic = lowestPrices.ToDictionarySafe(x => x.ProductId, x => x.LowestPrice);

                    foreach (var productId in productIdsChunk)
                    {
                        var lowestAttributeCombinationPrice = lowestPricesDic.GetValueOrDefault(productId);

                        // BatchUpdate recommended because products contain a lot of data (like full description).
                        await _db.Products
                            .Where(x => x.Id == productId)
                            .ExecuteUpdateAsync(
                                x => x.SetProperty(p => p.LowestAttributeCombinationPrice, p => lowestAttributeCombinationPrice));
                    }
                }
            }

            _requestCache.RemoveByPattern(ProductAttributeMaterializer.ATTRIBUTECOMBINATION_PATTERN_KEY);
        }
    }
}
