using System.Runtime.CompilerServices;

using Smartstore.Core.Data;
using Smartstore.Core.Seo;

namespace Smartstore.Core.Content.Topics
{
    public partial class TopicXmlSitemapPublisher : IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;

        public TopicXmlSitemapPublisher(
            SmartDbContext db,
            IWorkContext workContext)
        {
            _db = db;
            _workContext = workContext;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesTopics)
            {
                return null;
            }

            var customerRoleIds = _workContext.CurrentCustomer.GetRoleIds();

            var query = _db.Topics
                .AsNoTracking()
                .ApplyStandardFilter(false, customerRoleIds, context.RequestStoreId);

            return new TopicXmlSitemapResult { Query = query };
        }

        class TopicXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<Topic> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var topics = await Query.Select(x => new { x.Id }).ToListAsync(cancelToken);

                foreach (var x in topics)
                {
                    yield return new NamedEntity { EntityName = nameof(Topic), Id = x.Id, LastMod = DateTime.UtcNow };
                }
            }

            public override int Order => 200;
        }
    }
}