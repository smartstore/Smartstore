using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Catalog.Brands
{
    public partial class ManufacturerService : IManufacturerService, IXmlSitemapPublisher
    {
        // {0} = IncludeHidden, {1} = CustomerId, {2} = StoreId, {3} ProductIds
        private const string PRODUCTMANUFACTURERS_BYPRODUCTIDS_KEY = "productmanufacturer:byproductids-{0}-{1}-{2}-{3}";
        internal const string PRODUCTMANUFACTURERS_PATTERN_KEY = "productmanufacturer:*";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IRequestCache _requestCache;

        public ManufacturerService(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            IRequestCache requestCache)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _requestCache = requestCache;
        }

        public virtual async Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdsAsync(int[] productIds, bool includeHidden = false)
        {
            Guard.NotNull(productIds, nameof(productIds));

            if (!productIds.Any())
            {
                return new List<ProductManufacturer>();
            }

            var storeId = _storeContext.CurrentStore.Id;
            var cacheKey = PRODUCTMANUFACTURERS_BYPRODUCTIDS_KEY.FormatInvariant(includeHidden, _workContext.CurrentCustomer.Id, storeId, string.Join(",", productIds));

            var result = await _requestCache.GetAsync(cacheKey, async () =>
            {
                var customerRolesIds = includeHidden ? null : _workContext.CurrentCustomer.GetRoleIds();

                var manufacturersQuery = _db.Manufacturers
                    .AsNoTracking()
                    .ApplyStandardFilter(includeHidden, customerRolesIds, includeHidden ? 0 : storeId);

                var query =
                    from pm in _db.ProductManufacturers.AsNoTracking()
                    join m in manufacturersQuery on pm.ManufacturerId equals m.Id
                    where productIds.Contains(pm.ProductId)
                    orderby pm.DisplayOrder
                    select pm;

                var entities = await query.ToListAsync();
                return entities;
            });

            return result;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesManufacturers)
            {
                return null;
            }

            var customerRolesIds = _workContext.CurrentCustomer.GetRoleIds();

            var query = _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(false, customerRolesIds, context.RequestStoreId);

            return new ManufacturerXmlSitemapResult { Query = query };
        }


        class ManufacturerXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<Manufacturer> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var manufacturers = await Query.Select(x => new { x.Id, x.UpdatedOnUtc }).ToListAsync(cancelToken);

                await foreach (var x in manufacturers)
                {
                    yield return new NamedEntity { EntityName = nameof(Manufacturer), Id = x.Id, LastMod = x.UpdatedOnUtc };
                }
            }

            public override int Order => int.MinValue + 100;
        }
    }
}
