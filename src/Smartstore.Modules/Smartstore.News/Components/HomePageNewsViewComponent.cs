using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.News;
using Smartstore.News.Domain;
using Smartstore.News.Hooks;
using Smartstore.News.Models.Public;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render new section on the homepage.
    /// </summary>
    public class HomePageNewsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ICacheManager _cacheManager;
        private readonly NewsSettings _newsSettings;

        public HomePageNewsViewComponent(
            SmartDbContext db,
            ICommonServices services,
            ICacheManager cacheManager,
            NewsSettings newsSettings)
        {
            _db = db;
            _services = services;
            _cacheManager = cacheManager;
            _newsSettings = newsSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_newsSettings.Enabled || !_newsSettings.ShowNewsOnMainPage)
            {
                return Empty();
            }

            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var includeHidden = _services.WorkContext.CurrentCustomer.IsAdmin();
            var cacheKey = string.Format(ModelCacheInvalidator.HOMEPAGE_NEWSMODEL_KEY, languageId, storeId, _newsSettings.MainPageNewsCount, includeHidden);

            var cachedModel = await _cacheManager.GetAsync(cacheKey, async () =>
            {
                var newsItems = await _db.NewsItems()
                    .ApplyStandardFilter(storeId, languageId)
                    .ToPagedList(0, _newsSettings.MainPageNewsCount)
                    .LoadAsync();
                
                // TODO: (mh) (core) Remove this
                // INFO: Items will already be anounced in mapper
                //Services.DisplayControl.AnnounceRange(newsItems);

                return new HomePageNewsItemsModel
                {
                    NewsItems = await newsItems.SelectAsync(async x =>
                    {
                        var mapper = MapperFactory.GetMapper<NewsItem, PublicNewsItemModel>();
                        var newsItemModel = await mapper.MapAsync(x, new { PrepareComments = false });
                        return newsItemModel;
                    })
                    .AsyncToList()
                };
            });

            // "Comments" property of "NewsItemModel" object depends on the current customer.
            // Furthermore, we just don't need it for home page news. So let's update reset it.
            // But first we need to clone the cached model (the updated one should not be cached)
            var model = (HomePageNewsItemsModel)cachedModel.Clone();
            foreach (var newsItemModel in model.NewsItems)
            {
                newsItemModel.Comments.Comments.Clear();
            }

            return View(model);
        }
    }
}
