using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog.Hooks;
using Smartstore.Blog.Models.Public;
using Smartstore.Blog.Services;
using Smartstore.Core.Seo;
using Smartstore.Web.Components;

namespace Smartstore.Blog.Components
{
    /// <summary>
    /// Component to render tag navigation on the right side of blog item list.
    /// </summary>
    public class TagNavigationViewComponent : SmartViewComponent
    {
        private readonly IBlogService _blogService;
        private readonly BlogSettings _blogSettings;

        public TagNavigationViewComponent(IBlogService blogService, BlogSettings blogSettings)
        {
            _blogService = blogService;
            // INFO: (mh) (core) Why making things complicated and reinventing the wheel?! Also: if you don't have to, please don't resolve services from ICommonServices via .Resolve() or .SettingFactory.Load...
            _blogSettings = blogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;

            if (!_blogSettings.Enabled)
            {
                return Empty();
            }

            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var cacheKey = string.Format(ModelCacheInvalidator.BLOG_TAGS_MODEL_KEY, languageId, storeId);

            var cachedModel = await Services.Cache.GetAsync(cacheKey, async () =>
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
