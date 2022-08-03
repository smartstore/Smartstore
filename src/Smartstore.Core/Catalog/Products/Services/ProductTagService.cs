using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Catalog.Products
{
    public partial class ProductTagService : AsyncDbSaveHook<ProductTag>, IProductTagService
    {
        // {0} : include hidden, {1} : store ID, {2} : customer roles IDs.
        const string PRODUCTTAG_COUNT_KEY = "producttag:count-{0}-{1}-{2}";
        const string PRODUCTTAG_PATTERN_KEY = "producttag:*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly ICacheManager _cache;

        public ProductTagService(SmartDbContext db, IWorkContext workContext, ICacheManager cache)
        {
            _db = db;
            _workContext = workContext;
            _cache = cache;
        }

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            return _cache.RemoveByPatternAsync(PRODUCTTAG_PATTERN_KEY);
        }

        #endregion

        public virtual async Task UpdateProductTagsAsync(Product product, IEnumerable<string> tagNames)
        {
            Guard.NotNull(product, nameof(product));

            await _db.LoadCollectionAsync(product, x => x.ProductTags);

            // Clear cache only once at the end of this operation.
            using var scope = new DbContextScope(_db, minHookImportance: HookImportance.Important);

            if (!(tagNames?.Any() ?? false))
            {
                // Remove all tag mappings.
                if (product.ProductTags.Any())
                {
                    product.ProductTags.Clear();
                }
            }
            else
            {

                // Remove tag mappings.
                var tagsToRemove = new List<ProductTag>();
                var newTagNames = new HashSet<string>(tagNames
                    .Select(x => x.TrimSafe())
                    .Where(x => x.HasValue()),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var existingTag in product.ProductTags)
                {
                    var found = false;
                    foreach (var tagName in newTagNames)
                    {
                        if (existingTag.Name.EqualsNoCase(tagName))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        tagsToRemove.Add(existingTag);
                    }
                }

                foreach (var tag in tagsToRemove)
                {
                    product.ProductTags.Remove(tag);
                }

                // Add tag mappings.
                if (newTagNames.Any())
                {
                    var existingTags = await _db.ProductTags
                        .Where(x => newTagNames.Contains(x.Name))
                        .ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase);

                    // Tags must be saved and assigned an ID prior adding a mapping.
                    foreach (var name in newTagNames)
                    {
                        if (!existingTags.TryGetValue(name, out var tag))
                        {
                            tag = new ProductTag { Name = name, Published = true };

                            _db.ProductTags.Add(tag);
                            existingTags[name] = tag;
                        }
                    }

                    await _db.SaveChangesAsync();

                    foreach (var name in newTagNames)
                    {
                        if (existingTags.TryGetValue(name, out var tag) && !product.ProductTags.Any(x => x.Id == tag.Id))
                        {
                            product.ProductTags.Add(tag);
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }

            await _cache.RemoveByPatternAsync(PRODUCTTAG_PATTERN_KEY);
        }

        public virtual async Task<IDictionary<int, int>> GetProductCountsMapAsync(
            Customer customer = null,
            int storeId = 0,
            bool includeHidden = false)
        {
            customer ??= _workContext.CurrentCustomer;

            // TODO: ACL.
            // TODO: counting over navigation property is probably too slow. Mapping entity would be faster.

            //var roleIds = customer.CustomerRoleMappings
            //    .Select(x => x.CustomerRole)
            //    .Where(x => x.Active)
            //    .Select(x => x.Id)
            //    .ToArray();

            var storeToken = _db.QuerySettings.IgnoreMultiStore ? 0 : storeId;
            var rolesToken = "0";
            //var rolesToken = _db.QuerySettings.IgnoreAcl || includeHidden ? "0" : string.Join(",", roleIds);
            var cacheKey = PRODUCTTAG_COUNT_KEY.FormatInvariant(includeHidden.ToString().ToLower(), storeToken, rolesToken);

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
                    // Always count only published products.
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
