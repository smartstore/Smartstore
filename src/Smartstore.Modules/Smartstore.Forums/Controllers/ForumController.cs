using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Forums.Models.Mappers;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Controllers;

namespace Smartstore.Forums.Controllers
{
    // TODO: (mg) (core) add forum activity logs. They have never been used in frontend!?
    // TODO: (mg) (core) add check for store and ACL restrictions for subsequent entities (Forum, ForumTopic etc.) via associated ForumGroup.
    public class ForumController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ForumSettings _forumSettings;

        public ForumController(
            SmartDbContext db, 
            ForumSettings forumSettings)
        {
            _db = db;
            _forumSettings = forumSettings;
        }

        [LocalizedRoute("boards", Name = "Boards")]
        public async Task<IActionResult> Index()
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;

            var groups = await _db.ForumGroups()
                .Include(x => x.Forums)
                .AsNoTracking()
                .ApplyStoreFilter(store.Id)
                .ApplyAclFilter(customer)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var model = new PublicForumGroupListModel
            {
                CurrentTime = Services.DateTimeHelper.ConvertToUserTime(DateTime.UtcNow),
                MetaTitle = _forumSettings.GetLocalizedSetting(x => x.MetaTitle, store.Id).Value.NullEmpty() ?? T("Forum.PageTitle.Default"),
                MetaDescription = _forumSettings.GetLocalizedSetting(x => x.MetaDescription, store.Id),
                MetaKeywords = _forumSettings.GetLocalizedSetting(x => x.MetaKeywords, store.Id)
            };

            model.ForumGroups = await groups
                .SelectAsync(async x => await x.MapAsync())
                .AsyncToList();

            return View(model);
        }

        [LocalizedRoute("boards/forumgroup/{id:int}/{slug?}", Name = "ForumGroupBySlug")]
        public Task<IActionResult> ForumGroup(int id)
        {
            throw new NotImplementedException();
        }

        [LocalizedRoute("boards/topic/{id:int}/{slug?}", Name = "ForumTopicBySlug")]
        public Task<IActionResult> ForumTopic(int id)
        {
            throw new NotImplementedException();
        }

        [LocalizedRoute("boards/forum/{id:int}/{slug?}", Name = "ForumBySlug")]
        public Task<IActionResult> Forum(int id)
        {
            throw new NotImplementedException();
        }
    }
}
