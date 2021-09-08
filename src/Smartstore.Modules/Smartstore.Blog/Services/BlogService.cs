using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
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

        public virtual async Task UpdateCommentTotalsAsync(BlogPost blogPost)
        {
            Guard.NotNull(blogPost, nameof(blogPost));

            var approvedCommentCount = 0;
            var notApprovedCommentCount = 0;
            var blogComments = blogPost.BlogComments;

            foreach (var bc in blogComments)
            {
                if (bc.IsApproved)
                {
                    approvedCommentCount++;
                }
                else
                {
                    notApprovedCommentCount++;
                }
            }

            blogPost.ApprovedCommentCount = approvedCommentCount;
            blogPost.NotApprovedCommentCount = notApprovedCommentCount;

            await _db.SaveChangesAsync();
        }

        public virtual async Task<IList<BlogPostTag>> GetAllBlogPostTagsAsync(int storeId, int languageId = 0, bool includeHidden = false)
        {
            var blogPostTags = new List<BlogPostTag>();
            var blogPosts = await _db.BlogPosts()
                .ApplyStandardFilter(storeId, null, null, languageId, includeHidden)
                .ToListAsync();

            foreach (var blogPost in blogPosts)
            {
                var tags = blogPost.ParseTags();

                foreach (string tag in tags)
                {
                    var foundBlogPostTag = blogPostTags.Find(bpt => bpt.Name.Equals(tag, StringComparison.InvariantCultureIgnoreCase));

                    if (foundBlogPostTag == null)
                    {
                        foundBlogPostTag = new BlogPostTag
                        {
                            Name = tag,
                            BlogPostCount = 1
                        };

                        blogPostTags.Add(foundBlogPostTag);
                    }
                    else
                    {
                        foundBlogPostTag.BlogPostCount++;
                    }
                }
            }

            return blogPostTags;
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
                .ApplyStandardFilter(context.RequestStoreId, null, null);

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
