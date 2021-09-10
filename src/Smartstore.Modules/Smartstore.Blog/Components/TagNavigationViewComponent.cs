using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog;
using Smartstore.Blog.Hooks;
using Smartstore.Blog.Models.Public;
using Smartstore.Blog.Services;
using Smartstore.Caching;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render tag navigation on the right side of blog item list.
    /// </summary>
    public class TagNavigationViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IBlogService _blogService;
        private readonly ICommonServices _services;
        private readonly ICacheManager _cacheManager;
        private readonly BlogSettings _blogSettings;

        public TagNavigationViewComponent(
            SmartDbContext db,
            IBlogService blogService,
            ICommonServices services,
            ICacheManager cacheManager,
            BlogSettings blogSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _blogService = blogService;
            _services = services;
            _cacheManager = cacheManager;
            _blogSettings = blogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_blogSettings.Enabled)
            {
                return Empty();
            }

            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.BLOG_TAGS_MODEL_KEY, languageId, storeId);

            var cachedModel = await _cacheManager.GetAsync(cacheKey, async () =>
            {
                var model = new BlogPostTagListModel();

                var tags = (await _blogService.GetAllBlogPostTagsAsync(storeId, languageId))
                    .OrderByDescending(x => x.BlogPostCount)
                    .Take(_blogSettings.NumberOfTags)
                    .ToList();

                tags = tags.OrderBy(x => x.Name).ToList();

                foreach (var tag in tags)
                {
                    model.Tags.Add(new BlogPostTagModel
                    {
                        Name = tag.Name,        
                        SeName = SeoHelper.BuildSlug(tag.Name),
                        BlogPostCount = tag.BlogPostCount
                    });
                }

                return model;
            });

            return View(cachedModel);
        }
    }
}
