using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching.OutputCache;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.News.Domain;
using Smartstore.News.Models.Public;

namespace Smartstore.News.Controllers
{
    public partial class NewsHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly NewsSettings _newsSettings;

        public NewsHelper(
            SmartDbContext db,
            ICommonServices services,
            NewsSettings newsSettings)
        {
            _db = db;
            _services = services;
            _newsSettings = newsSettings;
        }

        public async Task<NewsItemListModel> PrepareNewsItemListModelAsync(NewsPagingFilteringModel command)
        {
            Guard.NotNull(command, nameof(command));

            if (command.PageSize <= 0)
                command.PageSize = _newsSettings.NewsArchivePageSize;
            if (command.PageNumber <= 0)
                command.PageNumber = 1;

            var model = await PrepareNewsItemListModelAsync(true, null, false, command.PageNumber - 1, command.PageSize, true);
            return model;
        }

        public async Task<NewsItemListModel> PrepareNewsItemListModelAsync(
            bool renderHeading,
            string newsHeading,
            bool disableCommentCount,
            int? pageIndex = null,
            int? maxPostAmount = null,
            bool displayPaging = false,
            int? maxAgeInDays = null)
        {
            var model = new NewsItemListModel
            {
                NewsHeading = newsHeading,
                RenderHeading = renderHeading,
                DisableCommentCount = disableCommentCount
            };

            var query = _db.NewsItems().AsNoTracking();

            if (maxAgeInDays.HasValue)
            {
                DateTime? maxAge = null;
                maxAge = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
                query = query.Where(n => n.CreatedOnUtc >= maxAge.Value);
            }

            var newsItems = await query
                .ApplyStandardFilter(_services.StoreContext.CurrentStore.Id, _services.WorkContext.WorkingLanguage.Id, _services.WorkContext.CurrentCustomer.IsAdmin())
                .ToPagedList(pageIndex ?? 0, maxPostAmount ?? _newsSettings.NewsArchivePageSize)
                .LoadAsync();

            if (displayPaging)
            {
                model.PagingFilteringContext.LoadPagedList(newsItems);
            }

            model.NewsItems = await newsItems
                .SelectAsync(async x =>
                {
                    var mapper = MapperFactory.GetMapper<NewsItem, PublicNewsItemModel>();
                    var newsItemModel = await mapper.MapAsync(x, new { PrepareComments = false });
                    return newsItemModel;
                })
                .AsyncToList();

            _services.DisplayControl.AnnounceRange(newsItems);

            return model;
        }
    }
}
