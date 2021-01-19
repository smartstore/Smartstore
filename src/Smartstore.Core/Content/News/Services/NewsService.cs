using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Data;

namespace Smartstore.Core.Content.News
{
    public partial class NewsService : INewsService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;

        public NewsService(
            SmartDbContext db,
            IWorkContext workContext)
        {
            _db = db;
            _workContext = workContext;
        }

        public virtual async Task UpdateCommentTotalsAsync(NewsItem newsItem)
        {
            Guard.NotNull(newsItem, nameof(newsItem));

            var approvedCommentCount = 0;
            var notApprovedCommentCount = 0;
            var newsComments = newsItem.NewsComments;

            foreach (var nc in newsComments)
            {
                if (nc.IsApproved)
                {
                    approvedCommentCount++;
                }
                else
                {
                    notApprovedCommentCount++;
                }
            }

            newsItem.ApprovedCommentCount = approvedCommentCount;
            newsItem.NotApprovedCommentCount = notApprovedCommentCount;

            await _db.SaveChangesAsync();
        }

        #region XML Sitemap

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesNews)
            {
                return null;
            }

            var query = _db.NewsItems
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

        #endregion
    }
}
