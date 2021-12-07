using System.Runtime.CompilerServices;
using Dasync.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;

namespace Smartstore.News.Services
{
    public partial class NewsItemXmlSitemapPublisher : IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;

        public NewsItemXmlSitemapPublisher(SmartDbContext db)
        {
            _db = db;
        }

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesNews)
            {
                return null;
            }

            var query = _db.NewsItems()
                .AsNoTracking()
                .ApplyStandardFilter(context.RequestStoreId);

            return new NewsItemXmlSitemapResult { Query = query };
        }

        class NewsItemXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<NewsItem> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var newsItems = await Query.Select(x => new { x.Id, x.CreatedOnUtc, x.LanguageId }).ToListAsync(cancelToken);

                await foreach (var x in newsItems)
                {
                    yield return new NamedEntity { EntityName = nameof(NewsItem), Id = x.Id, LastMod = x.CreatedOnUtc, LanguageId = x.LanguageId };
                }
            }

            public override int Order => 100;
        }
    }
}
