using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Mappers;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;

namespace Smartstore.Forums.Controllers
{
    // TODO: (mg) (core) add forum activity logs. They have never been used in frontend!?
    public class ForumController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly IBreadcrumb _breadcrumb;
        private readonly ForumSettings _forumSettings;
        private readonly SeoSettings _seoSettings;

        public ForumController(
            SmartDbContext db,
            IForumService forumService,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            IBreadcrumb breadcrumb,
            ForumSettings forumSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _forumService = forumService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _breadcrumb = breadcrumb;
            _forumSettings = forumSettings;
            _seoSettings = seoSettings;
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
        public async Task<IActionResult> ForumGroup(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var group = await _db.ForumGroups()
                .Include(x => x.Forums)
                .FindByIdAsync(id, false);

            if (group == null ||
                !await _storeMappingService.AuthorizeAsync(group) ||
                !await _aclService.AuthorizeAsync(group))
            {
                return NotFound();
            }

            var model = await group.MapAsync();
            await CreateForumBreadcrumb(group);

            return View(model);
        }

        [LocalizedRoute("boards/forum/{id:int}/{slug?}", Name = "ForumBySlug")]
        [LocalizedRoute("boards/forum/{id:int}/{slug?}/page/{page:int}", Name = "ForumBySlugPaged")]
        public async Task<IActionResult> Forum(int id, int page = 1)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (forum == null ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return NotFound();
            }

            var customer = Services.WorkContext.CurrentCustomer;
            var isGuest = customer.IsGuest();
            var pageSize = _forumSettings.TopicsPageSize > 0 ? _forumSettings.TopicsPageSize : 20;

            var topics = await _db.ForumTopics()
                .Include(x => x.Customer)
                .AsNoTracking()
                .ApplyStandardFilter(customer, forum.Id)
                .ToPagedList(page - 1, pageSize)
                .LoadAsync();

            var model = new PublicForumPageModel
            {
                Id = forum.Id,
                Name = forum.GetLocalized(x => x.Name),
                Slug = await forum.GetActiveSlugAsync(),
                Description = forum.GetLocalized(x => x.Description),
                TopicPageSize = topics.PageSize,
                TopicTotalRecords = topics.TotalCount,
                TopicPageIndex = topics.PageIndex,
                IsCustomerAllowedToSubscribe = !isGuest,
                ForumFeedsEnabled = _forumSettings.ForumFeedsEnabled,
                PostsPageSize = _forumSettings.PostsPageSize
            };

            if (!isGuest)
            {
                model.IsCustomerSubscribed = await _db.ForumSubscriptions()
                    .AsNoTracking()
                    .ApplyStandardFilter(customer.Id, forum.Id)
                    .FirstOrDefaultAsync() != null;
            }

            model.ForumTopics = await topics.MapAsync(_db);

            ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;

            await CreateForumBreadcrumb(null, forum);
            await SaveLastForumVisit(customer);

            return View(model);
        }

        public async Task<IActionResult> ForumRss(int id = 0)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (forum == null)
            {
                return NotFound();
            }
            
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var language = Services.WorkContext.WorkingLanguage;
            var protocol = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var selfLink = Url.Action("ForumRss", "Forum", null, protocol);
            var forumLink = Url.RouteUrl("ForumBySlug", new { id = forum.Id, slug = await forum.GetActiveSlugAsync() }, protocol);
            var feed = new SmartSyndicationFeed(new Uri(forumLink), store.Name, T("Forum.ForumFeedDescription"));

            if (!_forumSettings.ForumFeedsEnabled ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return new RssActionResult { Feed = feed };
            }

            feed.Title = new TextSyndicationContent("{0} - {1}".FormatInvariant(store.Name, forum.GetLocalized(x => x.Name, language)));

            string viewsText = T("Forum.Views");
            string repliesText = T("Forum.Replies");

            var topics = await _db.ForumTopics()
                .AsNoTracking()
                .ApplyStandardFilter(customer, forum.Id)
                .Take(_forumSettings.ForumFeedCount)
                .ToListAsync();

            feed.Items = topics.Select(x =>
            {
                var topicUrl = Url.RouteUrl("ForumTopicBySlug", new { id = x.Id, slug = _forumService.BuildSlug(x) }, protocol);
                var synopsis = "{0}: {1}, {2}: {3}".FormatInvariant(repliesText, x.NumReplies, viewsText, x.Views);

                return feed.CreateItem(x.Subject, synopsis, topicUrl, x.LastPostTime ?? x.UpdatedOnUtc);
            })
            .ToList();

            return new RssActionResult { Feed = feed };
        }

        [HttpPost]
        public async Task<IActionResult> ForumWatch(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (forum == null ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return NotFound();
            }

            var subscribed = false;
            string returnText = T("Forum.WatchForum");
            var customer = Services.WorkContext.CurrentCustomer;

            var subscription = await _db.ForumSubscriptions()
                .ApplyStandardFilter(customer.Id, forum.Id)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                _db.ForumSubscriptions().Add(new ForumSubscription
                {
                    SubscriptionGuid = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    ForumId = forum.Id,
                    CreatedOnUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                subscribed = true;
                returnText = T("Forum.UnwatchForum");
            }
            else
            {
                _db.ForumSubscriptions().Remove(subscription);
                await _db.SaveChangesAsync();

                subscribed = false;
            }

            return Json(new { Subscribed = subscribed, Text = returnText });
        }

        #region Topic

        [LocalizedRoute("boards/topic/{id:int}/{slug?}", Name = "ForumTopicBySlug")]
        public Task<IActionResult> ForumTopic(int id)
        {
            throw new NotImplementedException();
        }

        [GdprConsent]
        public Task<IActionResult> TopicCreate(int id)
        {
            throw new NotImplementedException();
        }

        #endregion

        private async Task CreateForumBreadcrumb(ForumGroup group = null, Forum forum = null, ForumTopic topic = null)
        {
            _breadcrumb.Track(new MenuItem
            {
                Text = T("Forum.Forums"),
                Rtl = Services.WorkContext.WorkingLanguage.Rtl,
                Url = Url.RouteUrl("Boards")
            });

            group ??= forum?.ForumGroup ?? topic?.Forum?.ForumGroup;
            if (group != null)
            {
                var groupName = group.GetLocalized(x => x.Name);
                _breadcrumb.Track(new MenuItem
                {
                    Text = groupName,
                    Rtl = groupName.CurrentLanguage.Rtl,
                    Url = Url.RouteUrl("ForumGroupBySlug", new { id = group.Id, slug = await group.GetActiveSlugAsync() })
                });
            }

            forum ??= topic?.Forum;
            if (forum != null)
            {
                var forumName = forum.GetLocalized(x => x.Name);
                _breadcrumb.Track(new MenuItem
                {
                    Text = forumName,
                    Rtl = forumName.CurrentLanguage.Rtl,
                    Url = Url.RouteUrl("ForumBySlug", new { id = forum.Id, slug = await forum.GetActiveSlugAsync() })
                });
            }

            if (topic != null)
            {
                _breadcrumb.Track(new MenuItem
                {
                    Text = topic.Subject,
                    Rtl = Services.WorkContext.WorkingLanguage.Rtl,
                    Url = Url.RouteUrl("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) })
                });
            }
        }

        private async Task SaveLastForumVisit(Customer customer)
        {
            try
            {
                if (!customer.Deleted && customer.Active && !customer.IsSystemAccount)
                {
                    var entry = _db.Entry(customer);

                    if (entry.State == EntityState.Detached)
                    {
                        entry.State = EntityState.Unchanged;
                    }

                    customer.LastForumVisit = DateTime.UtcNow;

                    await _db.SaveChangesAsync();                   
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
