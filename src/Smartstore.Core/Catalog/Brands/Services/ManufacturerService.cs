using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Brands
{
    public partial class ManufacturerService : IManufacturerService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;

        public ManufacturerService(
            SmartDbContext db,
            IWorkContext workContext)
        {
            _db = db;
            _workContext = workContext;
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
