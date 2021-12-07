using System.Runtime.CompilerServices;
using Dasync.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;

namespace Smartstore.Blog.Services
{
    public partial class BlogService : IBlogService, IXmlSitemapPublisher
    {
        private readonly SmartDbContext _db;

        public BlogService(SmartDbContext db)
        {
            _db = db;
        }

        public virtual async Task<ISet<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId = 0, bool includeHidden = false)
        {
            var blogPostTags = new List<BlogPostTag>();
            var tagsMap = new Dictionary<string, BlogPostTag>(StringComparer.OrdinalIgnoreCase);
            var blogPosts = await _db.BlogPosts()
                .ApplyStandardFilter(storeId, languageId, includeHidden)
                .Where(x => !string.IsNullOrEmpty(x.Tags))
                .Select(x => new { x.Id, x.Tags })
                .ToListAsync();

            foreach (var blogPost in blogPosts)
            {
                var tags = blogPost.Tags.SplitSafe(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (string tag in tags)
                {
                    if (tagsMap.TryGetValue(tag, out var postTag))
                    {
                        postTag.BlogPostCount++;
                    }
                    else
                    {
                        tagsMap[tag] = new BlogPostTag { Name = tag, BlogPostCount = 1 };
                    }
                }
            }

            return new HashSet<BlogPostTag>(tagsMap.Values);
        }

        #region XML Sitemap

        public XmlSitemapProvider PublishXmlSitemap(XmlSitemapBuildContext context)
        {
            if (!context.LoadSettings<SeoSettings>().XmlSitemapIncludesBlog)
            {
                return null;
            }

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(context.RequestStoreId);

            return new BlogPostXmlSitemapResult { Query = query };
        }

        class BlogPostXmlSitemapResult : XmlSitemapProvider
        {
            public IQueryable<BlogPost> Query { get; set; }

            public override async Task<int> GetTotalCountAsync()
            {
                return await Query.CountAsync();
            }

            public override async IAsyncEnumerable<NamedEntity> EnlistAsync([EnumeratorCancellation] CancellationToken cancelToken = default)
            {
                var blogPosts = await Query.Select(x => new { x.Id, x.CreatedOnUtc, x.LanguageId }).ToListAsync(cancelToken);

                await foreach (var x in blogPosts)
                {
                    yield return new NamedEntity { EntityName = nameof(BlogPost), Id = x.Id, LastMod = x.CreatedOnUtc, LanguageId = x.LanguageId };
                }
            }

            public override int Order => 300;
        }

        #endregion
    }
}
