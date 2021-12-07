using Microsoft.AspNetCore.Mvc;
using Smartstore.ComponentModel;
using Smartstore.News.Hooks;
using Smartstore.News.Models.Mappers;
using Smartstore.News.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.News.Components
{
    /// <summary>
    /// Component to render new section on the homepage.
    /// </summary>
    public class HomepageNewsViewComponent : SmartViewComponent
    {
        private readonly NewsSettings _newsSettings;

        public HomepageNewsViewComponent(NewsSettings newsSettings)
        {
            _newsSettings = newsSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            if (!_newsSettings.Enabled || !_newsSettings.ShowNewsOnMainPage)
            {
                return Empty();
            }

            var languageId = Services.WorkContext.WorkingLanguage.Id;
            var includeHidden = Services.WorkContext.CurrentCustomer.IsAdmin();
            var cacheKey = string.Format(ModelCacheInvalidator.HOMEPAGE_NEWSMODEL_KEY, languageId, storeId, _newsSettings.MainPageNewsCount, includeHidden);
            var cachedModel = await Services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async () =>
            {
                var newsItems = await Services.DbContext.NewsItems()
                    .AsNoTracking()
                    .ApplyStandardFilter(storeId, languageId)
                    .ToPagedList(0, _newsSettings.MainPageNewsCount)
                    .LoadAsync();

                return new HomepageNewsItemsModel
                {
                    NewsItems = await newsItems.SelectAsync(async x =>
                    {
                        return await x.MapAsync(new { PrepareComments = false });
                    })
                    .AsyncToList()
                };
            });

            // "Comments" property of "NewsItemModel" object depends on the current customer.
            // Furthermore, we just don't need it for home page news. So let's update reset it.
            // But first we need to clone the cached model (the updated one should not be cached)
            var model = (HomepageNewsItemsModel)cachedModel.Clone();
            foreach (var newsItemModel in model.NewsItems)
            {
                newsItemModel.Comments.Comments.Clear();
            }

            return View(model);
        }
    }
}
