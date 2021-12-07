using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Forums.Models.Mappers;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    /// <summary>
    /// Component to render active discussions on forum homepage.
    /// </summary>
    public class ActiveDiscussionsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ForumSettings _forumSettings;

        public ActiveDiscussionsViewComponent(
            SmartDbContext db,
            ICommonServices services,
            ForumSettings forumSettings)
        {
            _db = db;
            _services = services;
            _forumSettings = forumSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            bool isActiveDiscussionsPage = false, 
            int? forumId = null,
            int? pageIndex = null)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return Empty();
            }

            var store = _services.StoreContext.CurrentStore;
            var currentCustomer = _services.WorkContext.CurrentCustomer;

            var pageSize = isActiveDiscussionsPage
                ? _forumSettings.ActiveDiscussionsPageTopicCount
                : _forumSettings.HomePageActiveDiscussionsTopicCount;

            var topics = await _db.ForumTopics()
                .IncludeCustomer()
                .AsNoTracking()
                .ApplyActiveFilter(store, currentCustomer, forumId)
                .ToPagedList(pageIndex ?? 0, pageSize)
                .LoadAsync();

            if (!isActiveDiscussionsPage && !topics.Any())
            {
                return Empty();
            }

            var model = new ActiveDiscussionsModel
            {
                IsActiveDiscussionsPage = isActiveDiscussionsPage,
                ActiveDiscussionsFeedEnabled = _forumSettings.ActiveDiscussionsFeedEnabled,
                ForumTopics = await topics.MapAsync(_db)
            };

            return View(model);
        }
    }
}
