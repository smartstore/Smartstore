using System.Linq;
using System.Threading.Tasks;
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

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return Empty();
            }

            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;

            var topics = await _db.ForumTopics()
                .Include(x => x.Customer)
                .AsNoTracking()
                .ApplyActiveFilter(store, customer)
                .Take(_forumSettings.HomePageActiveDiscussionsTopicCount)
                .ToListAsync();

            if (!topics.Any())
            {
                return Empty();
            }

            var lastPostIds = topics
                .Where(x => x.LastPostId != 0)
                .Select(x => x.LastPostId)
                .Distinct()
                .ToArray();

            var lastPosts = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .Include(x => x.Customer)
                .AsNoTracking()
                .Where(x => lastPostIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var model = new ActiveDiscussionsModel
            {
                IsForumGroupsPage = true,
                ActiveDiscussionsFeedEnabled = _forumSettings.ActiveDiscussionsFeedEnabled,
                PostsPageSize = _forumSettings.PostsPageSize
            };

            model.ForumTopics = await topics
                .SelectAsync(async x => await x.MapAsync(lastPosts, null))
                .AsyncToList();

            ViewBag.ShowTopic = false;

            return View(model);
        }
    }
}
