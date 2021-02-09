using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductTagService : IProductTagService
    {
        // {0} : store ID, {1} : include hidden.
        private const string PRODUCTTAG_COUNT_KEY = "producttag:count-{0}-{1}";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;

        public ProductTagService(
            SmartDbContext db,
            ICacheManager cache)
        {
            _db = db;
            _cache = cache;
        }

        protected virtual async Task<Dictionary<int, int>> GetProductsPerTagCountsAsync(int storeId, bool includeHidden)
        {
            var cacheKey = PRODUCTTAG_COUNT_KEY.FormatInvariant(_db.QuerySettings.IgnoreMultiStore ? 0 : storeId, includeHidden.ToString().ToLower());

            var result = await _cache.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(3));

                List<ProductsPerTag> counts;
                var query = _db.ProductTags.AsNoTracking();

                if (!includeHidden)
                {
                    query = query.Where(x => x.Published);
                }

                if (storeId > 0 && !_db.QuerySettings.IgnoreMultiStore)
                {
                    counts = await query.Select(tag => new ProductsPerTag
                    {
                        ProductTagId = tag.Id,
                        ProductCount = 
                            (from p in tag.Products
                            join sm in _db.StoreMappings.AsNoTracking() on new { pid = p.Id, pname = "Product" } equals new { pid = sm.EntityId, pname = sm.EntityName } into psm
                            from sm in psm.DefaultIfEmpty()
                            where (!p.LimitedToStores || storeId == sm.StoreId) &&
                                p.Visibility == ProductVisibility.Full &&
                                p.Published &&
                                !p.IsSystemProduct &&
                                (includeHidden || tag.Published)
                            select p).Count()

                    })
                    .ToListAsync();
                }
                else
                {
                    counts = await query.Select(tag => new ProductsPerTag
                    {
                        ProductTagId = tag.Id,
                        ProductCount = tag.Products.Count(p => 
                            p.Visibility == ProductVisibility.Full && 
                            p.Published && 
                            !p.IsSystemProduct && 
                            (includeHidden || tag.Published))
                    })
                    .ToListAsync();
                }

                return counts.ToDictionary(x => x.ProductTagId, x => x.ProductCount);
            });

            return result;
        }

        private class ProductsPerTag
        {
            public int ProductTagId { get; set; }
            public int ProductCount { get; set; }
        }
    }
}
