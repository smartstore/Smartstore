using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Models.Mappers;
using Smartstore.Blog.Models.Public;
using Smartstore.Caching.OutputCache;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Blog.Controllers
{
    public partial class BlogHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly BlogSettings _blogSettings;
        
        public BlogHelper(SmartDbContext db, ICommonServices services, BlogSettings blogSettings)
        {
            _db = db;
            _services = services;
            _blogSettings = blogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public async Task<BlogPostListModel> PrepareBlogPostListModelAsync(BlogPagingFilteringModel command)
        {
            Guard.NotNull(command, nameof(command));

            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel();
            model.PagingFilteringContext.Tag = command.Tag;
            model.PagingFilteringContext.Month = command.Month;

            if (command.PageSize <= 0)
                command.PageSize = _blogSettings.PostsPageSize;
            if (command.PageNumber <= 0)
                command.PageNumber = 1;

            DateTime? dateFrom = command.GetFromMonth();
            DateTime? dateTo = command.GetToMonth();

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(storeId, languageId, isAdmin)
                .AsQueryable();

            if (!command.Tag.HasValue())
            {
                query = query.ApplyTimeFilter(dateFrom, dateTo);
            }

            var blogPosts = command.Tag.HasValue()
                ? (await query.ToListAsync())
                    .FilterByTag(command.Tag)
                    .ToPagedList(command.PageNumber - 1, command.PageSize)
                : query.ToPagedList(command.PageNumber - 1, command.PageSize);

            var pagedBlogPosts = await blogPosts.LoadAsync();

            model.PagingFilteringContext.LoadPagedList(pagedBlogPosts);

            // Prepare SEO model.
            var parsedMonth = model.PagingFilteringContext.GetParsedMonth();
            var tag = model.PagingFilteringContext.Tag;

            if (parsedMonth == null && tag == null)
            {
                model.MetaTitle = _blogSettings.GetLocalizedSetting(x => x.MetaTitle, storeId);
                model.MetaDescription = _blogSettings.GetLocalizedSetting(x => x.MetaDescription, storeId);
                model.MetaKeywords = _blogSettings.GetLocalizedSetting(x => x.MetaKeywords, storeId);
            }
            else
            {
                var month = parsedMonth != null ? $"{parsedMonth.Value.ToNativeString("MMMM", CultureInfo.InvariantCulture)} {parsedMonth.Value.Year}" : string.Empty;
                model.MetaTitle = parsedMonth != null ? T("PageTitle.Blog.Month", month) : T("PageTitle.Blog.Tag", tag);
                model.MetaDescription = parsedMonth != null ? T("Metadesc.Blog.Month", month) : T("Metadesc.Blog.Tag", tag);
                model.MetaKeywords = parsedMonth != null ? month : tag;
            }

            model.StoreName = _services.StoreContext.CurrentStore.Name;

            _services.DisplayControl.AnnounceRange(pagedBlogPosts);

            model.BlogPosts = await pagedBlogPosts
                .SelectAsync(async x =>
                {
                    return await x.MapAsync(new { PrepareComments = false });
                })
                .AsyncToList();

            return model;
        }

        public async Task<BlogPostListModel> PrepareBlogPostListModelAsync(
            int? maxPostAmount,
            int? maxAgeInDays,
            bool renderHeading,
            string blogHeading,
            bool disableCommentCount,
            string postsWithTag)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel
            {
                BlogHeading = blogHeading,
                RenderHeading = renderHeading,
                RssToLinkButton = renderHeading,
                DisableCommentCount = disableCommentCount
            };

            DateTime? maxAge = null;
            if (maxAgeInDays.HasValue)
            {
                maxAge = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
            }

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(storeId, languageId, isAdmin)
                .ApplyTimeFilter(maxAge: maxAge)
                .AsQueryable();

            var blogPosts = await query.ToListAsync();

            if (!postsWithTag.IsEmpty())
            {
                blogPosts = blogPosts.FilterByTag(postsWithTag).ToList();
            }

            var pagedBlogPosts = await blogPosts
                .ToPagedList(0, maxPostAmount ?? 100)
                .LoadAsync();

            _services.DisplayControl.AnnounceRange(blogPosts);

            model.BlogPosts = await blogPosts
                .SelectAsync(async x =>
                {
                    return await x.MapAsync(new { PrepareComments = false });
                })
                .AsyncToList();

            return model;
        }
    }
}
