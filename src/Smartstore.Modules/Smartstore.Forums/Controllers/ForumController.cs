using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Web.Controllers;

namespace Smartstore.Forums.Controllers
{
    // TODO: (mg) (core) add forum activity logs. They have never been used in frontend!?
    public class ForumController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly ForumSettings _forumSettings;
        private readonly CustomerSettings _customerSettings;

        public ForumController(
            SmartDbContext db, 
            IForumService forumService, 
            ForumSettings forumSettings,
            CustomerSettings customerSettings)
        {
            _db = db;
            _forumService = forumService;
            _forumSettings = forumSettings;
            _customerSettings = customerSettings;
        }

        [LocalizedRoute("forum", Name = "Forum")]
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
                .SelectAsync(async x => await PrepareForumGroupModel(x))
                .AsyncToList();

            return View(model);
        }

        [LocalizedRoute("forum/group/{id:int}/{slug?}", Name = "ForumGroupBySlug")]
        public Task<IActionResult> ForumGroup(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IActionResult> ForumTopic(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IActionResult> Forum(int id)
        {
            throw new NotImplementedException();
        }


        private async Task<PublicForumGroupModel> PrepareForumGroupModel(ForumGroup group)
        {
            var model = new PublicForumGroupModel
            {
                Id = group.Id,
                Name = group.GetLocalized(x => x.Name),
                Description = group.GetLocalized(x => x.Description),
                Slug = group.GetActiveSlug()
            };

            var lastPostIds = group.Forums
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

            model.Forums = group.Forums
                .OrderBy(x => x.DisplayOrder)
                .Select(x => PrepareForumModel(x, lastPosts))
                .ToList();

            return model;
        }

        private PublicForumModel PrepareForumModel(Forum forum, Dictionary<int, ForumPost> lastPosts)
        {
            var model = new PublicForumModel
            {
                Id = forum.Id,
                Name = forum.GetLocalized(x => x.Name),
                Slug = forum.GetActiveSlug(),
                Description = forum.GetLocalized(x => x.Description),
                NumTopics = forum.NumTopics,
                NumPosts = forum.NumPosts,
                LastPostId = forum.LastPostId
            };

            if (forum.LastPostId != 0 && lastPosts.TryGetValue(forum.LastPostId, out var lastPost))
            {
                model.LastPost = PreparePostModel(lastPost);
            }

            return model;
        }

        private PublicForumPostModel PreparePostModel(ForumPost post)
        {
            var createdOn = Services.DateTimeHelper.ConvertToUserTime(post.CreatedOnUtc, DateTimeKind.Utc);

            var model = new PublicForumPostModel
            {
                Id = post.Id,
                ForumTopicId = post.TopicId,
                ForumTopicSlug = _forumService.BuildSlug(post.ForumTopic),
                ForumTopicSubject = _forumService.StripSubject(post.ForumTopic),
                CustomerId = post.CustomerId,
                AllowViewingProfiles = _customerSettings.AllowViewingProfiles,
                CustomerName = post.Customer.FormatUserName(true),
                IsCustomerGuest = post.Customer.IsGuest(),
                Published = post.Published,
                PostCreatedOnStr = _forumSettings.RelativeDateTimeFormattingEnabled
                    ? post.CreatedOnUtc.Humanize(false)
                    : createdOn.ToString("f")
            };

            return model;
        }
    }
}
