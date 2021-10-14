using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore.Collections;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Messaging;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Forums.Domain;
using Smartstore.Forums.Models.Mappers;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Search;
using Smartstore.Forums.Services;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Search;

namespace Smartstore.Forums.Controllers
{
    public partial class BoardsController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IForumService _forumService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly IBreadcrumb _breadcrumb;
        private readonly IMessageFactory _messageFactory;
        private readonly IForumSearchService _forumSearchService;
        private readonly Lazy<IFacetTemplateProvider> _facetTemplateProvider;
        private readonly ForumSettings _forumSettings;
        private readonly SeoSettings _seoSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly ForumSearchSettings _searchSettings;

        public BoardsController(
            SmartDbContext db,
            IForumService forumService,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            IBreadcrumb breadcrumb,
            IMessageFactory messageFactory,
            IForumSearchService forumSearchService,
            Lazy<IFacetTemplateProvider> facetTemplateProvider,
            ForumSettings forumSettings,
            SeoSettings seoSettings,
            CaptchaSettings captchaSettings,
            ForumSearchSettings searchSettings)
        {
            _db = db;
            _forumService = forumService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _breadcrumb = breadcrumb;
            _messageFactory = messageFactory;
            _forumSearchService = forumSearchService;
            _facetTemplateProvider = facetTemplateProvider;
            _forumSettings = forumSettings;
            _seoSettings = seoSettings;
            _captchaSettings = captchaSettings;
            _searchSettings = searchSettings;
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

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var pageSize = _forumSettings.TopicsPageSize > 0 ? _forumSettings.TopicsPageSize : 20;

            var topics = await _db.ForumTopics()
                .IncludeCustomer()
                .AsNoTracking()
                .ApplyStandardFilter(currentCustomer, forum.Id)
                .ToPagedList(page - 1, pageSize)
                .LoadAsync();

            var model = new PublicForumPageModel
            {
                Id = forum.Id,
                Name = forum.GetLocalized(x => x.Name),
                Slug = await forum.GetActiveSlugAsync(),
                Description = forum.GetLocalized(x => x.Description),
                CanSubscribe = !currentCustomer.IsGuest(),
                ForumFeedsEnabled = _forumSettings.ForumFeedsEnabled
            };

            if (model.CanSubscribe)
            {
                model.IsSubscribed = await _db.ForumSubscriptions()
                    .AsNoTracking()
                    .ApplyStandardFilter(currentCustomer.Id, forum.Id)
                    .FirstOrDefaultAsync() != null;
            }

            model.ForumTopics = await topics.MapAsync(_db);

            ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;

            await CreateForumBreadcrumb(null, forum);
            await SaveLastForumVisit(currentCustomer, null);

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
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var language = Services.WorkContext.WorkingLanguage;
            var protocol = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var selfLink = Url.Action("ForumRss", "Boards", null, protocol);
            var forumLink = Url.RouteUrl("ForumBySlug", new { id = forum.Id, slug = await forum.GetActiveSlugAsync() }, protocol);
            var feed = new SmartSyndicationFeed(new Uri(forumLink), store.Name, T("Forum.ForumFeedDescription"));

            feed.AddNamespaces(false);
            feed.Init(selfLink, language.LanguageCulture.EmptyNull().ToLower());

            if (!_forumSettings.ForumFeedsEnabled ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return new RssActionResult(feed);
            }

            feed.Title = new TextSyndicationContent($"{store.Name} - {forum.GetLocalized(x => x.Name, language)}");

            string viewsText = T("Forum.Views");
            string repliesText = T("Forum.Replies");

            var topics = await _db.ForumTopics()
                .AsNoTracking()
                .ApplyStandardFilter(currentCustomer, forum.Id)
                .Take(_forumSettings.ForumFeedCount)
                .ToListAsync();

            feed.Items = topics.Select(x =>
            {
                var topicUrl = Url.RouteUrl("ForumTopicBySlug", new { id = x.Id, slug = _forumService.BuildSlug(x) }, protocol);
                var synopsis = $"{repliesText}: {x.NumReplies}, {viewsText}: {x.Views}";

                return feed.CreateItem(x.Subject, synopsis, topicUrl, x.LastPostTime ?? x.UpdatedOnUtc);
            })
            .ToList();

            return new RssActionResult(feed);
        }

        [HttpPost]
        public async Task<IActionResult> ForumWatch(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (forum == null ||
                currentCustomer.IsGuest() ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return NotFound();
            }

            var subscribed = false;
            string returnText = T("Forum.WatchForum");

            var subscription = await _db.ForumSubscriptions()
                .ApplyStandardFilter(currentCustomer.Id, forum.Id)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                _db.ForumSubscriptions().Add(new ForumSubscription
                {
                    SubscriptionGuid = Guid.NewGuid(),
                    CustomerId = currentCustomer.Id,
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

        [Route("boards/activediscussions", Name = "ForumActiveDiscussions")]
        [Route("boards/activediscussions/page/{page:int}", Name = "ForumActiveDiscussionsPaged")]
        public IActionResult ActiveDiscussions(int? forumId = null, int page = 1)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            ViewBag.ForumId = forumId;
            ViewBag.PageIndex = page - 1;

            return View();
        }

        public async Task<IActionResult> ActiveDiscussionsRss(int? forumId = null)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var store = Services.StoreContext.CurrentStore;
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var language = Services.WorkContext.WorkingLanguage;
            var protocol = Services.WebHelper.IsCurrentConnectionSecured() ? "https" : "http";
            var selfLink = Url.Action("ActiveDiscussionsRss", "Boards", null, protocol);
            var discussionLink = Url.RouteUrl("ForumActiveDiscussions", null, protocol);
            var feed = new SmartSyndicationFeed(new Uri(discussionLink), $"{store.Name} - {T("Forum.ActiveDiscussionsFeedTitle")}", T("Forum.ActiveDiscussionsFeedDescription"));

            feed.AddNamespaces(false);
            feed.Init(selfLink, language.LanguageCulture.EmptyNull().ToLower());

            if (!_forumSettings.ActiveDiscussionsFeedEnabled)
            {
                return new RssActionResult(feed);
            }

            string viewsText = T("Forum.Views");
            string repliesText = T("Forum.Replies");

            var topics = await _db.ForumTopics()
                .AsNoTracking()
                .ApplyActiveFilter(store, currentCustomer, forumId)
                .Take(_forumSettings.ActiveDiscussionsFeedCount)
                .ToListAsync();

            feed.Items = topics.Select(x =>
            {
                var topicUrl = Url.RouteUrl("ForumTopicBySlug", new { id = x.Id, slug = _forumService.BuildSlug(x) }, protocol);
                var synopsis = $"{repliesText}: {x.NumReplies}, {viewsText}: {x.Views}";

                return feed.CreateItem(x.Subject, synopsis, topicUrl, x.LastPostTime ?? x.UpdatedOnUtc);
            })
            .ToList();

            return new RssActionResult(feed);
        }

        #region Forum topic

        [LocalizedRoute("boards/topic/{id:int}/{slug?}", Name = "ForumTopicBySlug")]
        [LocalizedRoute("boards/topic/{id:int}/{slug?}/page/{page:int}", Name = "ForumTopicBySlugPaged")]
        public async Task<IActionResult> Topic(int id, int page = 1)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var posts = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .Include(x => x.ForumPostVotes)
                .IncludeCustomer()
                .AsNoTracking()
                .ApplyStandardFilter(currentCustomer, topic.Id)
                .ToPagedList(page - 1, _forumSettings.PostsPageSize)
                .LoadAsync();

            // If no posts area loaded, redirect to the first page.
            if (posts.Count == 0 && page > 1)
            {
                return RedirectToRoute("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });
            }

            var model = new PublicForumTopicPageModel
            {
                Id = topic.Id,
                Subject = topic.Subject,
                Slug = _forumService.BuildSlug(topic),
                ModerationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer),
                CanSubscribe = !currentCustomer.IsGuest()
            };

            if (model.CanSubscribe)
            {
                model.IsSubscribed = await _db.ForumSubscriptions()
                    .AsNoTracking()
                    .ApplyStandardFilter(currentCustomer.Id, null, topic.Id)
                    .FirstOrDefaultAsync() != null;
            }

            var postModels = await posts
                .SelectAsync(async x => await x.MapAsync(false, true, page))
                .AsyncToList();

            model.ForumPosts = new PagedList<PublicForumPostModel>(postModels, posts.PageIndex, posts.PageSize, posts.TotalCount);

            await CreateForumBreadcrumb(topic: topic);
            await SaveLastForumVisit(currentCustomer, topic, true);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TopicWatch(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var subscribed = false;
            string returnText = T("Forum.WatchTopic");

            var subscription = await _db.ForumSubscriptions()
                .ApplyStandardFilter(currentCustomer.Id, null, topic.Id)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                _db.ForumSubscriptions().Add(new ForumSubscription
                {
                    SubscriptionGuid = Guid.NewGuid(),
                    CustomerId = currentCustomer.Id,
                    TopicId = topic.Id,
                    CreatedOnUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                subscribed = true;
                returnText = T("Forum.UnwatchTopic");
            }
            else
            {
                _db.ForumSubscriptions().Remove(subscription);
                await _db.SaveChangesAsync();

                subscribed = false;
            }

            return Json(new { Subscribed = subscribed, Text = returnText });
        }

        public async Task<IActionResult> TopicMove(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var store = Services.StoreContext.CurrentStore;
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var model = new PublicMoveForumTopicModel
            {
                Id = topic.Id,
                SelectedForumId = topic.ForumId,
                CustomerId = topic.CustomerId,
                TopicSlug = _forumService.BuildSlug(topic),
                ModerationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer)
            };

            if (!model.CanMoveTopic && currentCustomer.Id != topic.CustomerId)
            {
                return Unauthorized();
            }

            var forums = new List<SelectListItem>();
            var groups = await _db.ForumGroups()
                .Include(x => x.Forums)
                .AsNoTracking()
                .ApplyStoreFilter(store.Id)
                .ApplyAclFilter(currentCustomer)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();
            
            foreach (var group in groups)
            {
                var optGroup = new SelectListGroup { Name = group.GetLocalized(x => x.Name) };

                foreach (var forum in group.Forums.OrderBy(x => x.DisplayOrder))
                {
                    forums.Add(new SelectListItem
                    {
                        Text = forum.GetLocalized(x => x.Name),
                        Value = forum.Id.ToString(),
                        Group = optGroup
                    });
                }
            }

            ViewBag.Forums = forums;

            await CreateForumBreadcrumb(topic: topic);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TopicMove(PublicMoveForumTopicModel model)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(model.Id);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanMoveTopic))
            {
                return Unauthorized();
            }

            var oldForumId = topic.ForumId;
            var newForumId = model.SelectedForumId;

            if (oldForumId != newForumId && await _db.Forums().AnyAsync(x => x.Id == newForumId))
            {
                topic.ForumId = newForumId;
                await _db.SaveChangesAsync();
            }

            return RedirectToRoute("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });
        }

        [GdprConsent]
        public async Task<IActionResult> TopicCreate(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (forum == null ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return NotFound();
            }

            var model = await CreateEditForumTopicModel(forum);
            if (!model.CanCreateTopics)
            {
                return Unauthorized();
            }

            await CreateForumBreadcrumb(forum: forum);

            ViewBag.ForumTopicTypes = CreateForumTopicTypes();

            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        [GdprConsent]
        public async Task<IActionResult> TopicCreate(PublicEditForumTopicModel model, string captchaError)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var forum = await _db.Forums()
                .Include(x => x.ForumGroup)
                .FindByIdAsync(model.ForumId);

            if (forum == null ||
                !await _storeMappingService.AuthorizeAsync(forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(forum.ForumGroup))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(null, null, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanCreateTopics))
            {
                return Unauthorized();
            }

            if (_captchaSettings.ShowOnForumPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var topic = new ForumTopic
                    {
                        ForumId = forum.Id,
                        CustomerId = currentCustomer.Id,
                        Published = true,
                        TopicTypeId = (int)ForumTopicType.Normal
                    };

                    if (currentCustomer.IsForumModerator())
                    {
                        topic.Published = model.Published;
                        topic.TopicTypeId = model.TopicTypeId;
                    }

                    topic.Subject = _forumSettings.TopicSubjectMaxLength > 0 && model.Subject.Length > _forumSettings.TopicSubjectMaxLength
                        ? model.Subject.Substring(0, _forumSettings.TopicSubjectMaxLength)
                        : model.Subject;

                    _db.ForumTopics().Add(topic);
                    // We need the topic ID.
                    await _db.SaveChangesAsync();
                    
                    // Send topic notification to subscriber.
                    var languageId = Services.WorkContext.WorkingLanguage.Id;
                    var subscriptionPager = _db.ForumSubscriptions()
                        .Include(x => x.Customer)
                        .AsNoTracking()
                        .Where(x => x.CustomerId != topic.CustomerId)
                        .ApplyStandardFilter(null, forum.Id)
                        .ToFastPager();

                    while ((await subscriptionPager.ReadNextPageAsync<ForumSubscription>()).Out(out var subscriptions))
                    {
                        foreach (var subscription in subscriptions)
                        {
                            if (subscription.Customer.Email.HasValue())
                            {
                                await _messageFactory.SendNewForumTopicMessageAsync(subscription.Customer, topic, languageId);
                            }
                        }
                    }

                    // Add forum post.
                    var post = new ForumPost
                    {
                        TopicId = topic.Id,
                        CustomerId = currentCustomer.Id,
                        IPAddress = Services.WebHelper.GetClientIpAddress().ToString(),
                        Published = true
                    };

                    post.Text = _forumSettings.PostMaxLength > 0 && model.Text.Length > _forumSettings.PostMaxLength
                        ? model.Text.Substring(0, _forumSettings.PostMaxLength)
                        : model.Text;

                    _db.ForumPosts().Add(post);
                    // We need the post ID.
                    await _db.SaveChangesAsync();

                    topic.NumPosts = topic.Published ? 1 : 0;
                    topic.LastPostId = post.Id;
                    topic.LastPostCustomerId = post.CustomerId;
                    topic.LastPostTime = post.CreatedOnUtc;

                    // Subscription.
                    if (model.IsSubscribed && !currentCustomer.IsGuest())
                    {
                        _db.ForumSubscriptions().Add(new ForumSubscription
                        {
                            SubscriptionGuid = Guid.NewGuid(),
                            CustomerId = currentCustomer.Id,
                            TopicId = topic.Id,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                    }

                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreAddForumTopic, T("ActivityLog.PublicStore.AddForumTopic"), topic.Subject);

                    return RedirectToRoute("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            // Redisplay form.
            model.Id = 0;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage;
            model.ForumId = forum.Id;
            model.ForumName = forum.GetLocalized(x => x.Name);
            model.ForumSlug = await forum.GetActiveSlugAsync();
            model.ForumEditor = _forumSettings.ForumEditor;
            model.CustomerId = currentCustomer.Id;
            model.IsModerator = currentCustomer.IsForumModerator();
            model.ModerationPermissions = _forumService.GetModerationPermissions(null, null, currentCustomer);

            ViewBag.ForumTopicTypes = CreateForumTopicTypes();

            return View(model);
        }

        public async Task<IActionResult> TopicEdit(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var model = await CreateEditForumTopicModel(topic.Forum, topic);
            if (!model.CanEditTopic)
            {
                return Unauthorized();
            }

            await CreateForumBreadcrumb(null, topic.Forum, topic);

            ViewBag.ForumTopicTypes = CreateForumTopicTypes();

            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        public async Task<IActionResult> TopicEdit(PublicEditForumTopicModel model, string captchaError)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(model.Id);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanEditTopic))
            {
                return Unauthorized();
            }

            if (_captchaSettings.ShowOnForumPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Topic.
                    if (currentCustomer.IsForumModerator())
                    {
                        topic.Published = model.Published;
                        topic.TopicTypeId = model.TopicTypeId;
                    }

                    topic.Subject = _forumSettings.TopicSubjectMaxLength > 0 && model.Subject.Length > _forumSettings.TopicSubjectMaxLength
                        ? model.Subject.Substring(0, _forumSettings.TopicSubjectMaxLength)
                        : model.Subject;

                    // First post.
                    var text = _forumSettings.PostMaxLength > 0 && model.Text.Length > _forumSettings.PostMaxLength
                        ? model.Text.Substring(0, _forumSettings.PostMaxLength)
                        : model.Text;

                    var firstPost = await _db.ForumPosts()
                        .AsNoTracking()
                        .ApplyStandardFilter(currentCustomer, topic.Id)
                        .FirstOrDefaultAsync();

                    if (firstPost != null)
                    {
                        firstPost.Text = text;
                    }
                    else
                    {
                        firstPost = new ForumPost
                        {
                            TopicId = topic.Id,
                            CustomerId = topic.CustomerId,
                            Text = text,
                            IPAddress = Services.WebHelper.GetClientIpAddress().ToString(),
                            Published = true
                        };

                        _db.ForumPosts().Add(firstPost);
                    }

                    await ApplySubscription(topic.Id, model.IsSubscribed);

                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreEditForumTopic, T("ActivityLog.PublicStore.EditForumTopic"), topic.Subject);

                    return RedirectToRoute("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            // Redisplay form.
            model.Published = topic.Published;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage;
            model.ForumName = topic.Forum.GetLocalized(x => x.Name);
            model.ForumSlug = await topic.Forum.GetActiveSlugAsync();
            model.ForumId = topic.Forum.Id;
            model.ForumEditor = _forumSettings.ForumEditor;
            model.CustomerId = currentCustomer.Id;
            model.IsModerator = currentCustomer.IsForumModerator();
            model.ModerationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer);

            ViewBag.ForumTopicTypes = CreateForumTopicTypes();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TopicDelete(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics().FindByIdAsync(id);

            if (!await IsTopicVisible(topic, currentCustomer))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanDeleteTopic))
            {
                return Unauthorized();
            }

            var forumId = topic.ForumId;

            _db.ForumTopics().Remove(topic);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreDeleteForumTopic, T("ActivityLog.PublicStore.DeleteForumTopic"), topic.Subject);

            var forum = await _db.Forums().FindByIdAsync(forumId, false);
            if (forum != null)
            {
                return RedirectToRoute("ForumBySlug", new { id = forum.Id, slug = await forum.GetActiveSlugAsync() });
            }

            return RedirectToRoute("Boards");
        }

        private async Task<bool> IsTopicVisible(ForumTopic topic, Customer customer)
        {
            if (topic == null)
            {
                return false;
            }

            if (!topic.Published && topic.CustomerId != customer.Id && !customer.IsForumModerator())
            {
                return false;
            }

            if (!await _storeMappingService.AuthorizeAsync(topic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(topic.Forum.ForumGroup))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Forum post

        [HttpGet]
        [GdprConsent]
        [Route("boards/postcreate/{id:int}/{quote?}", Name = "ForumPostCreate")]
        public async Task<IActionResult> PostCreate(int id, int? quote)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (topic == null ||
                !await _storeMappingService.AuthorizeAsync(topic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(topic.Forum.ForumGroup))
            {
                return NotFound();
            }

            var model = await CreateEditForumPostModel(topic);
            if (!model.CanCreatePosts)
            {
                return Unauthorized();
            }

            // Insert the quoted text.
            if (quote.HasValue)
            {
                var quotePost = await _db.ForumPosts()
                    .IncludeCustomer()
                    .FindByIdAsync(quote.Value, false);

                if (quotePost != null && quotePost.TopicId == topic.Id)
                {
                    var userName = quotePost.Customer.IsGuest()
                        ? T("Customer.Guest").Value
                        : quotePost.Customer.FormatUserName();

                    switch (_forumSettings.ForumEditor)
                    {
                        case EditorType.SimpleTextBox:
                            model.Text = $"{userName.NaIfEmpty()}:\n{quotePost.Text}\n";
                            break;
                        case EditorType.BBCodeEditor:
                            model.Text = $"[quote={userName.NaIfEmpty()}]{BBCodeHelper.RemoveQuotes(quotePost.Text)}[/quote]";
                            break;
                    }
                }
            }

            await CreateForumBreadcrumb(null, topic.Forum, topic);
            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        [GdprConsent]
        [Route("boards/postcreate/{id:int}/{quote?}", Name = "ForumPostCreate")]
        public async Task<IActionResult> PostCreate(PublicEditForumPostModel model, string captchaError)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var topic = await _db.ForumTopics()
                .Include(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(model.ForumTopicId, false);

            if (topic == null ||
                !await _storeMappingService.AuthorizeAsync(topic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(topic.Forum.ForumGroup))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanCreatePosts))
            {
                return Unauthorized();
            }

            if (_captchaSettings.ShowOnForumPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var post = new ForumPost
                    {
                        TopicId = topic.Id,
                        CustomerId = currentCustomer.Id,
                        IPAddress = Services.WebHelper.GetClientIpAddress().ToString(),
                        Published = true
                    };

                    if (currentCustomer.IsForumModerator())
                    {
                        post.Published = model.Published;
                    }

                    post.Text = _forumSettings.PostMaxLength > 0 && model.Text.Length > _forumSettings.PostMaxLength
                        ? model.Text.Substring(0, _forumSettings.PostMaxLength)
                        : model.Text;

                    _db.ForumPosts().Add(post);

                    await ApplySubscription(topic.Id, model.IsSubscribed);

                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreAddForumPost, T("ActivityLog.PublicStore.AddForumPost"), post.Text.Truncate(100, "…"));

                    var pageSize = _forumSettings.PostsPageSize > 0 ? _forumSettings.PostsPageSize : 20;
                    var pageIndex = await _db.ForumPosts().GetTopicPageIndexAsync(currentCustomer, topic.Id, pageSize, post.Id) + 1;

                    var url = pageIndex > 1
                        ? Url.RouteUrl("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic), page = pageIndex })
                        : Url.RouteUrl("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });

                    return Redirect($"{url}#{post.Id}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            // Redisplay form.
            model.Id = 0;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage;
            model.ForumName = topic.Forum.GetLocalized(x => x.Name);
            model.ForumId = topic.Forum.Id;
            model.ForumSlug = await topic.Forum.GetActiveSlugAsync();
            model.ForumTopicId = topic.Id;
            model.ForumTopicSubject = topic.Subject;
            model.ForumTopicSlug = _forumService.BuildSlug(topic);
            model.ForumEditor = _forumSettings.ForumEditor;
            model.IsModerator = currentCustomer.IsForumModerator();
            model.CanSubscribe = !currentCustomer.IsGuest();

            return View(model);
        }

        public async Task<IActionResult> PostEdit(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var post = await _db.ForumPosts()
                .Include(x => x.Customer)
                .Include(x => x.ForumTopic)
                .ThenInclude(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id, false);

            if (post == null ||
                !await _storeMappingService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup))
            {
                return NotFound();
            }

            var model = await CreateEditForumPostModel(post.ForumTopic, post);
            if (!model.CanEditPost)
            {
                return Unauthorized();
            }

            await CreateForumBreadcrumb(null, post.ForumTopic.Forum, post.ForumTopic);

            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        public async Task<IActionResult> PostEdit(PublicEditForumPostModel model, string captchaError)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var post = await _db.ForumPosts()
                .Include(x => x.Customer)
                .Include(x => x.ForumTopic)
                .ThenInclude(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(model.Id);

            if (post == null ||
                !await _storeMappingService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(null, post, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanEditPost))
            {
                return Unauthorized();
            }

            if (_captchaSettings.ShowOnForumPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (currentCustomer.IsForumModerator())
                    {
                        // Do not allow to unpublish first post. NumReplies would be wrong. Unpublish topic instead.
                        var firstPost = await _db.ForumPosts()
                            .AsNoTracking()
                            .ApplyStandardFilter(currentCustomer, post.TopicId)
                            .FirstOrDefaultAsync();
                        if (firstPost?.Id != post.Id)
                        {
                            post.Published = model.Published;
                        }
                    }

                    post.Text = _forumSettings.PostMaxLength > 0 && model.Text.Length > _forumSettings.PostMaxLength
                        ? model.Text.Substring(0, _forumSettings.PostMaxLength)
                        : model.Text;

                    await ApplySubscription(post.TopicId, model.IsSubscribed);

                    await _db.SaveChangesAsync();

                    Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreEditForumPost, T("ActivityLog.PublicStore.EditForumPost"), post.Text.Truncate(100, "…"));

                    var pageSize = _forumSettings.PostsPageSize > 0 ? _forumSettings.PostsPageSize : 20;
                    var pageIndex = await _db.ForumPosts().GetTopicPageIndexAsync(currentCustomer, post.TopicId, pageSize, post.Id) + 1;

                    var url = pageIndex > 1
                        ? Url.RouteUrl("ForumTopicBySlug", new { id = post.TopicId, slug = _forumService.BuildSlug(post.ForumTopic), page = pageIndex })
                        : Url.RouteUrl("ForumTopicBySlug", new { id = post.TopicId, slug = _forumService.BuildSlug(post.ForumTopic) });

                    return Redirect($"{url}#{post.Id}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            // Redisplay form.
            model.Published = post.Published;
            model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage;
            model.ForumName = post.ForumTopic.Forum.GetLocalized(x => x.Name);
            model.ForumSlug = await post.ForumTopic.Forum.GetActiveSlugAsync();
            model.ForumId = post.ForumTopic.Forum.Id;
            model.ForumTopicId = post.ForumTopic.Id;
            model.ForumTopicSubject = post.ForumTopic.Subject;
            model.ForumTopicSlug = _forumService.BuildSlug(post.ForumTopic);
            model.Id = post.Id;
            model.ForumEditor = _forumSettings.ForumEditor;
            model.CustomerId = currentCustomer.Id;
            model.IsModerator = currentCustomer.IsForumModerator();
            model.CanSubscribe = !currentCustomer.IsGuest();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PostDelete(int id)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var post = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .ThenInclude(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .FindByIdAsync(id);

            if (post == null ||
                !await _storeMappingService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup))
            {
                return NotFound();
            }

            var moderationPermissions = _forumService.GetModerationPermissions(null, post, currentCustomer);
            if (!moderationPermissions.HasFlag(ForumModerationPermissionFlags.CanDeletePost))
            {
                return Unauthorized();
            }

            var topicId = post.TopicId;
            var forumId = post.ForumTopic.Forum.Id;
            var forumSlug = await post.ForumTopic.Forum.GetActiveSlugAsync();
            var postText = post.Text;

            _db.ForumPosts().Remove(post);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(ForumActivityLogTypes.PublicStoreDeleteForumPost, T("ActivityLog.PublicStore.DeleteForumPost"), postText.Truncate(100, "…"));

            // Get topic one more time because it can be deleted (first or only post deleted).
            var topic = await _db.ForumTopics().FindByIdAsync(topicId, false);

            if (topic == null)
            {
                return RedirectToRoute("ForumBySlug", new { id = forumId, slug = forumSlug });
            }
            else
            {
                return RedirectToRoute("ForumTopicBySlug", new { id = topic.Id, slug = _forumService.BuildSlug(topic) });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostVote(int id, bool vote)
        {
            if (!_forumSettings.ForumsEnabled || !_forumSettings.AllowCustomersToVoteOnPosts)
            {
                return NotFound();
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var post = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .ThenInclude(x => x.Forum)
                .ThenInclude(x => x.ForumGroup)
                .Include(x => x.ForumPostVotes)
                .FindByIdAsync(id);

            if (post == null ||
                !await _storeMappingService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup) ||
                !await _aclService.AuthorizeAsync(post.ForumTopic.Forum.ForumGroup))
            {
                return NotFound();
            }

            if (!_forumSettings.AllowGuestsToVoteOnPosts && currentCustomer.IsGuest())
            {
                return Json(new { success = false, message = T("Forum.Post.Vote.OnlyRegistered").Value });
            }

            // Do not allow to vote for own posts.
            if (post.CustomerId == currentCustomer.Id)
            {
                return Json(new { success = false, message = T("Forum.Post.Vote.OwnPostNotAllowed").Value });
            }

            var voteEntity = post.ForumPostVotes.FirstOrDefault(x => x.CustomerId == currentCustomer.Id);
            var voteCount = post.ForumPostVotes.Count;

            if (vote)
            {
                if (voteEntity == null)
                {
                    _db.CustomerContent.Add(new ForumPostVote
                    {
                        ForumPostId = post.Id,
                        Vote = true,
                        CustomerId = currentCustomer.Id,
                        IpAddress = Services.WebHelper.GetClientIpAddress().ToString()
                    });

                    ++voteCount;
                }
                else
                {
                    voteEntity.Vote = true;
                }
            }
            else
            {
                if (voteEntity != null)
                {
                    _db.CustomerContent.Remove(voteEntity);
                    --voteCount;
                }
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = T("Forum.Post.Vote.SuccessfullyVoted").Value,
                voteCount,
                voteCountString = voteCount.ToString("N0")
            });
        }

        #endregion

        #region Search

        [HttpPost]
        [LocalizedRoute("boards/instantsearch", Name = "ForumInstantSearch")]
        public async Task<IActionResult> InstantSearch(ForumSearchQuery query)
        {
            if (!_forumSettings.ForumsEnabled || string.IsNullOrWhiteSpace(query.Term) || query.Term.Length < _searchSettings.InstantSearchTermMinLength)
            {
                return new EmptyResult();
            }

            query = query
                .BuildFacetMap(false)
                .Slice(0, Math.Min(16, _searchSettings.InstantSearchNumberOfHits))
                .SortBy(ForumTopicSorting.Relevance);

            var result = await _forumSearchService.SearchAsync(query);

            var model = new ForumSearchResultModel(query)
            {
                SearchResult = result,
                Term = query.Term,
                TotalCount = result.TotalHitsCount
            };

            var hits = await result.GetHitsAsync();

            if (hits.Any())
            {
                var processedIds = new HashSet<int>();
                var hitGroup = new SearchResultModelBase.HitGroup(model)
                {
                    Name = "InstantSearchHits",
                    DisplayName = T("Search.Hits"),
                    Ordinal = 1
                };

                foreach (var post in hits)
                {
                    if (processedIds.Add(post.TopicId))
                    {
                        hitGroup.Hits.Add(new SearchResultModelBase.HitItem
                        {
                            Label = post.ForumTopic.Subject,
                            Url = Url.RouteUrl("ForumTopicBySlug", new { id = post.TopicId, slug = _forumService.BuildSlug(post.ForumTopic) }) + "#" + post.Id
                        });
                    }
                }

                model.HitGroups.Add(hitGroup);
            }

            model.AddSpellCheckerSuggestions(result.SpellCheckerSuggestions, T, x => Url.RouteUrl("ForumSearch", new { q = x }));

            return PartialView(model);
        }

        [HttpGet]
        [LocalizedRoute("boards/search", Name = "ForumSearch")]
        public async Task<IActionResult> Search(ForumSearchQuery query)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            ForumSearchResult result = null;
            var language = Services.WorkContext.WorkingLanguage;
            var model = new ForumSearchResultModel(query)
            {
                AllowSorting = _forumSettings.AllowSorting
            };

            // Sorting.
            if (model.AllowSorting)
            {
                model.CurrentSortOrder = query?.CustomData.Get("CurrentSortOrder").Convert<int?>();

                model.AvailableSortOptions = Services.Cache.Get($"pres:forumsortoptions-{language.Id}", () =>
                {
                    var dict = new Dictionary<int, string>();
                    foreach (ForumTopicSorting val in Enum.GetValues(typeof(ForumTopicSorting)))
                    {
                        if (val != ForumTopicSorting.Initial)
                        {
                            dict[(int)val] = Services.Localization.GetLocalizedEnum(val, language.Id);
                        }
                    }

                    return dict;
                });

                if (model.CurrentSortOrderName.IsEmpty())
                {
                    model.CurrentSortOrderName = model.AvailableSortOptions.Get(model.CurrentSortOrder ?? 1) ?? model.AvailableSortOptions.First().Value;
                }
            }

            if (query.Term.HasValue() && query.Term.Length < _searchSettings.InstantSearchTermMinLength)
            {
                model.SearchResult = new ForumSearchResult(query);
                model.Error = T("Search.SearchTermMinimumLengthIsNCharacters", _searchSettings.InstantSearchTermMinLength);

                return View(model);
            }

            try
            {
                if (query.Term.HasValue())
                {
                    result = await _forumSearchService.SearchAsync(query);

                    if (result.TotalHitsCount == 0 && result.SpellCheckerSuggestions.Any())
                    {
                        // No matches, but spell checker made a suggestion.
                        // We implicitly search again with the first suggested term.
                        var oldSuggestions = result.SpellCheckerSuggestions;
                        var oldTerm = query.Term;
                        query.Term = oldSuggestions[0];

                        result = await _forumSearchService.SearchAsync(query);

                        if (result.TotalHitsCount > 0)
                        {
                            model.AttemptedTerm = oldTerm;
                            // Restore the original suggestions.
                            result.SpellCheckerSuggestions = oldSuggestions.Where(x => x != query.Term).ToArray();
                        }
                        else
                        {
                            query.Term = oldTerm;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                model.Error = ex.ToString();
            }

            model.SearchResult = result ?? new ForumSearchResult(query);
            model.Term = query.Term;
            model.TotalCount = model.SearchResult.TotalHitsCount;

            // Set facet counters to 0 because they refer to posts, not topics, and would confuse here.
            foreach (var group in model.SearchResult.Facets.Values)
            {
                group.Facets.Each(x => x.HitCount = 0);
            }

            await PrepareSearchResult(model, null);

            await CreateForumBreadcrumb();
            _breadcrumb.Track(new MenuItem { Text = T("Forum.Search") });

            ViewBag.TemplateProvider = _facetTemplateProvider.Value;

            return View(model);
        }

        // AJAX.
        [HttpPost]
        public async Task<IActionResult> Search(ForumSearchQuery query, int[] renderedTopicIds)
        {
            if (!_forumSettings.ForumsEnabled || query.Term.IsEmpty() || query.Term.Length < _searchSettings.InstantSearchTermMinLength)
            {
                return new EmptyResult();
            }

            query = query.BuildFacetMap(false).CheckSpelling(0);

            var model = new ForumSearchResultModel(query);

            try
            {
                model.SearchResult = await _forumSearchService.SearchAsync(query);
            }
            catch (Exception ex)
            {
                model.SearchResult = new ForumSearchResult(query);
                model.Error = ex.ToString();
            }

            model.Term = query.Term;
            model.TotalCount = model.SearchResult.TotalHitsCount;

            await PrepareSearchResult(model, renderedTopicIds);

            return PartialView("_SearchHits", model);
        }

        private async Task PrepareSearchResult(ForumSearchResultModel model, int[] renderedTopicIds)
        {
            // The search result may contain duplicate topics.
            // Make sure that no topic is rendered more than once.
            var hits = await model.SearchResult.GetHitsAsync();
            var lastPosts = await _db.ForumPosts().GetLastForumPostsAsync(hits.Select(x => x.ForumTopic.LastPostId));
            var renderedIds = new HashSet<int>(renderedTopicIds ?? Array.Empty<int>());
            var hitModels = new List<PublicForumTopicModel>();

            foreach (var post in hits)
            {
                if (renderedIds.Add(post.TopicId))
                {
                    var hitModel = await post.ForumTopic.MapAsync(lastPosts, post);
                    hitModels.Add(hitModel);
                }
            }

            model.PagedList = new PagedList<PublicForumTopicModel>(
                hitModels,
                hits.PageIndex,
                hits.PageSize,
                model.TotalCount);

            model.CumulativeHitCount = renderedIds.Count;
        }

        #endregion

        #region Customer subscriptions

        public async Task<IActionResult> CustomerSubscriptions(int? page)
        {
            if (!_forumSettings.AllowCustomersToManageSubscriptions)
            {
                return RedirectToAction("Info", "Customer");
            }

            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var pageIndex = (page ?? 1) - 1;
            var pageSize = _forumSettings.ForumSubscriptionsPageSize;

            var subscriptions = await _db.ForumSubscriptions()
                .AsNoTracking()
                .ApplyStandardFilter(currentCustomer.Id)
                .ToPagedList(pageIndex, pageSize)
                .LoadAsync();

            var subscriptionModels = await subscriptions.SelectAsync(async x =>
            {
                var sm = new CustomerForumSubscriptionModel
                {
                    Id = x.Id,
                    ForumTopicId = x.TopicId,
                    ForumId = x.ForumId,
                    TopicSubscription = x.TopicId > 0,
                    Title = string.Empty,
                    Slug = string.Empty
                };

                if (x.TopicId > 0)
                {
                    var forumTopic = await _db.ForumTopics().FindByIdAsync(x.TopicId, false);
                    if (forumTopic != null)
                    {
                        sm.Title = forumTopic.Subject;
                        sm.Slug = _forumService.BuildSlug(forumTopic);
                    }
                }
                else
                {
                    var forum = await _db.Forums().FindByIdAsync(x.ForumId, false);
                    if (forum != null)
                    {
                        sm.Title = forum.GetLocalized(x => x.Name);
                        sm.Slug = await forum.GetActiveSlugAsync();
                    }
                }

                return sm;
            })
            .AsyncToList();

            var model = new CustomerForumSubscriptionsModel
            {
                ForumSubscriptions = subscriptionModels
                    .ToPagedList(subscriptions.PageIndex, subscriptions.PageSize, subscriptions.TotalCount)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CustomerSubscriptions(IFormCollection form)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;
            var toDeleteIds = new HashSet<int>();

            foreach (var key in form.Keys)
            {
                var value = form[key].FirstOrDefault();

                if (value.EqualsNoCase("on") && key.StartsWith("fs", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (int.TryParse(key.Replace("fs", string.Empty).Trim(), out var subscriptionId))
                    {
                        toDeleteIds.Add(subscriptionId);
                    }
                }
            }

            if (toDeleteIds.Any())
            {
                var toDelete = await _db.ForumSubscriptions()
                    .Where(x => toDeleteIds.Contains(x.Id) && x.CustomerId == currentCustomer.Id)
                    .ToListAsync();

                if (toDelete.Any())
                {
                    _db.ForumSubscriptions().RemoveRange(toDelete);
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(CustomerSubscriptions));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomerSubscription(int id)
        {
            var subscription = await _db.ForumSubscriptions().FindByIdAsync(id);
            if (subscription == null || subscription.CustomerId != Services.WorkContext.CurrentCustomer.Id)
            {
                return NotFound();
            }

            _db.ForumSubscriptions().Remove(subscription);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(CustomerSubscriptions));
        }

        #endregion

        #region Customer profile

        public async Task<IActionResult> LatestForumPosts(int id /* customerId */, int? page)
        {
            if (!_forumSettings.ForumsEnabled)
            {
                return NotFound();
            }

            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(id, false);

            if (customer == null)
            {
                return NotFound();
            }

            var pageIndex = (page ?? 1) - 1;
            var pageSize = _forumSettings.LatestCustomerPostsPageSize;

            var latestPosts = await _db.ForumPosts()
                .Include(x => x.ForumTopic)
                .AsNoTracking()
                .Where(x => x.CustomerId == customer.Id)
                .ApplyStandardFilter(customer, null, false, true)
                .ToPagedList(pageIndex, pageSize)
                .LoadAsync();

            var latestPostsModels = latestPosts
                .Select(x =>
                {
                    var createdOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);

                    var postModel = new CustomerProfilePostModel
                    {
                        Id = x.Id,
                        ForumTopicId = x.TopicId,
                        ForumTopicSubject = x.ForumTopic.Subject,
                        ForumTopicSlug = _forumService.BuildSlug(x.ForumTopic),
                        ForumPostText = _forumService.FormatPostText(x),
                        PostCreatedOnStr = _forumSettings.RelativeDateTimeFormattingEnabled
                            ? createdOn.Humanize(false)
                            : createdOn.ToString("f")
                    };

                    return postModel;
                })
                .ToList();

            var model = new CustomerProfilePostsModel
            {
                Id = id,
                LatestPosts = latestPostsModels.ToPagedList(latestPosts.PageIndex, latestPosts.PageSize, latestPosts.TotalCount)
            };

            return PartialView(model);
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

        private async Task<int> SaveLastForumVisit(Customer customer, ForumTopic topic, bool updateViewCount = false)
        {
            var num = 0;
            try
            {
                if (!customer.Deleted && customer.Active && !customer.IsSystemAccount)
                {
                    var entry = _db.Entry(customer);

                    if (entry.State == EntityState.Detached)
                    {
                        entry.State = EntityState.Unchanged;
                    }

                    using var scope = new Data.DbContextScope(_db, minHookImportance: HookImportance.Important);

                    customer.LastForumVisit = DateTime.UtcNow;

                    if (updateViewCount && topic != null)
                    {
                        topic.Views += 1;
                    }

                    num = await _db.SaveChangesAsync();                   
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return num;
        }

        private async Task<PublicEditForumTopicModel> CreateEditForumTopicModel(Forum forum, ForumTopic topic = null)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;

            var model = new PublicEditForumTopicModel
            {
                Id = topic?.Id ?? 0,
                Published = topic?.Published ?? true,
                Slug = _forumService.BuildSlug(topic),
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage,
                ForumId = forum.Id,
                ForumName = forum.GetLocalized(x => x.Name),
                ForumSlug = await forum.GetActiveSlugAsync(),
                Subject = topic?.Subject,
                TopicTypeId = topic?.TopicTypeId ?? 0,
                ForumEditor = _forumSettings.ForumEditor,
                CustomerId = topic?.CustomerId ?? 0,
                IsModerator = currentCustomer.IsForumModerator(),
                ModerationPermissions = _forumService.GetModerationPermissions(topic, null, currentCustomer),
                CanSubscribe = !currentCustomer.IsGuest()
            };

            if (topic != null)
            {
                if (model.CanSubscribe)
                {
                    model.IsSubscribed = await _db.ForumSubscriptions()
                        .AsNoTracking()
                        .ApplyStandardFilter(currentCustomer.Id, null, topic.Id)
                        .FirstOrDefaultAsync() != null;
                }

                var firstPost = await _db.ForumPosts()
                    .AsNoTracking()
                    .ApplyStandardFilter(currentCustomer, topic.Id)
                    .FirstOrDefaultAsync();

                model.Text = firstPost?.Text;
            }

            return model;
        }

        private async Task<PublicEditForumPostModel> CreateEditForumPostModel(ForumTopic topic, ForumPost post = null)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;

            var model = new PublicEditForumPostModel
            {
                Id = post?.Id ?? 0,
                Published = post?.Published ?? true,
                ForumTopicId = topic.Id,
                DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnForumPage,
                ForumEditor = _forumSettings.ForumEditor,
                ForumName = topic.Forum.GetLocalized(x => x.Name),
                ForumSlug = await topic.Forum.GetActiveSlugAsync(),
                ForumId = topic.Forum.Id,
                ForumTopicSubject = topic.Subject,
                ForumTopicSlug = _forumService.BuildSlug(topic),
                Text = post?.Text,
                CustomerId = currentCustomer.Id,
                IsModerator = currentCustomer.IsForumModerator(),
                ModerationPermissions = _forumService.GetModerationPermissions(topic, post, currentCustomer),
                CanSubscribe = !currentCustomer.IsGuest()
            };

            if (model.CanSubscribe)
            {
                model.IsSubscribed = await _db.ForumSubscriptions()
                    .AsNoTracking()
                    .ApplyStandardFilter(currentCustomer.Id, null, topic.Id)
                    .FirstOrDefaultAsync() != null;
            }

            if (post != null)
            {
                var firstPost = await _db.ForumPosts()
                    .AsNoTracking()
                    .ApplyStandardFilter(currentCustomer, topic.Id)
                    .FirstOrDefaultAsync();

                model.IsFirstPost = firstPost?.Id == post.Id;
            }

            return model;
        }

        private async Task ApplySubscription(int topicId, bool subscribe)
        {
            var currentCustomer = Services.WorkContext.CurrentCustomer;

            if (topicId != 0 && !currentCustomer.IsGuest())
            {
                var subscription = await _db.ForumSubscriptions()
                    .ApplyStandardFilter(currentCustomer.Id, null, topicId)
                    .FirstOrDefaultAsync();

                if (subscribe)
                {
                    if (subscription == null)
                    {
                        _db.ForumSubscriptions().Add(new ForumSubscription
                        {
                            SubscriptionGuid = Guid.NewGuid(),
                            CustomerId = currentCustomer.Id,
                            TopicId = topicId,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                    }
                }
                else
                {
                    if (subscription != null)
                    {
                        _db.ForumSubscriptions().Remove(subscription);
                    }
                }
            }
        }

        private List<SelectListItem> CreateForumTopicTypes()
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = T("Forum.Normal"),
                    Value = ((int)ForumTopicType.Normal).ToString()
                },
                new SelectListItem
                {
                    Text = T("Forum.Sticky"),
                    Value = ((int)ForumTopicType.Sticky).ToString()
                },
                new SelectListItem
                {
                    Text = T("Forum.Announcement"),
                    Value = ((int)ForumTopicType.Announcement).ToString()
                }
            };
        }
    }
}
