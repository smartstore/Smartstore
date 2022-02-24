using System.ServiceModel.Syndication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Smartstore.Blog.Messaging;
using Smartstore.Blog.Models.Mappers;
using Smartstore.Blog.Models.Public;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Http;
using Smartstore.Net;
using Smartstore.Utilities.Html;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;

namespace Smartstore.Blog.Controllers
{
    public class BlogController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IPageAssetBuilder _pageAssetBuilder;
        private readonly BlogHelper _helper;
        private readonly Lazy<IWebHelper> _webHelper;
        private readonly Lazy<IActivityLogger> _activityLogger;
        private readonly Lazy<IMessageFactory> _messageFactory;
        private readonly BlogSettings _blogSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly SeoSettings _seoSettings;

        public BlogController(
            SmartDbContext db,
            ICommonServices services,
            IStoreMappingService storeMappingService,
            IPageAssetBuilder pageAssetBuilder,
            BlogHelper helper,
            Lazy<IWebHelper> webHelper,
            Lazy<IActivityLogger> activityLogger,
            Lazy<IMessageFactory> messageFactory,
            BlogSettings blogSettings,
            LocalizationSettings localizationSettings,
            CaptchaSettings captchaSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _services = services;
            _storeMappingService = storeMappingService;
            _pageAssetBuilder = pageAssetBuilder;
            _helper = helper;
            _webHelper = webHelper;
            _activityLogger = activityLogger;
            _messageFactory = messageFactory;
            _blogSettings = blogSettings;
            _localizationSettings = localizationSettings;
            _captchaSettings = captchaSettings;
            _seoSettings = seoSettings;
        }

        [LocalizedRoute("blog", Name = "Blog")]
        public async Task<IActionResult> List(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            var model = await _helper.PrepareBlogPostListModelAsync(command);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                var url = Url.RouteUrl("Blog", null, Request.Scheme);
                _pageAssetBuilder.AppendCanonicalUrlParts(url);
            }

            return View(model);
        }

        public async Task<IActionResult> BlogSummary(
            int? maxPostAmount,
            int? maxAgeInDays,
            bool renderHeading,
            string blogHeading,
            bool disableCommentCount,
            string postsWithTag)
        {
            var model = await _helper.PrepareBlogPostListModelAsync(maxPostAmount, maxAgeInDays, renderHeading, blogHeading, disableCommentCount, postsWithTag);

            return PartialView(model);
        }

        [LocalizedRoute("blog/tag/{tag}", Name = "BlogByTag")]
        public async Task<IActionResult> BlogByTag(string tag, BlogPagingFilteringModel command)
        {
            // INFO: param 'tag' redundant, because OutputCache does not include
            // complex type params in cache key computing

            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                var blogUrl = Url.RouteUrl("BlogByTag", new { tag }, Request.Scheme);
                _pageAssetBuilder.AppendCanonicalUrlParts(blogUrl);
            }

            var model = await _helper.PrepareBlogPostListModelAsync(command);

            return View(nameof(List), model);
        }

        [LocalizedRoute("blog/month/{month}", Name = "BlogByMonth")]
        public async Task<IActionResult> BlogByMonth(string month, BlogPagingFilteringModel command)
        {
            // INFO: param 'month' redundant, because OutputCache does not include
            // complex type params in cache key computing

            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                var blogUrl = Url.RouteUrl("BlogByMonth", new { month }, Request.Scheme);
                _pageAssetBuilder.AppendCanonicalUrlParts(blogUrl);
            }

            var model = await _helper.PrepareBlogPostListModelAsync(command);

            return View(nameof(List), model);
        }

        [LocalizedRoute("/blog/rss", Name = "BlogRSS")]
        public async Task<IActionResult> ListRss()
        {
            DateTime? maxAge = null;
            var protocol = _webHelper.Value.IsCurrentConnectionSecured() ? "https" : "http";
            var selfLink = Url.RouteUrl("BlogRSS", null, protocol);
            var blogLink = Url.RouteUrl("Blog", null, protocol);
            var language = _services.WorkContext.WorkingLanguage;
            var store = _services.StoreContext.CurrentStore;
            var title = $"{store.Name} - Blog";

            if (_blogSettings.MaxAgeInDays > 0)
            {
                maxAge = DateTime.UtcNow.Subtract(new TimeSpan(_blogSettings.MaxAgeInDays, 0, 0, 0));
            }

            var feed = new SmartSyndicationFeed(new Uri(blogLink), title);
            feed.AddNamespaces(false);
            feed.Init(selfLink, language.LanguageCulture.EmptyNull().ToLower());

            if (!_blogSettings.Enabled)
            {
                return new RssActionResult(feed);
            }

            var items = new List<SyndicationItem>();
            var blogPosts = await _db.BlogPosts()
                .AsNoTracking()
                .ApplyTimeFilter(maxAge)
                .ApplyStandardFilter(store.Id, language.Id)
                .ToListAsync();

            foreach (var blogPost in blogPosts)
            {
                var blogPostUrl = Url.RouteUrl("BlogPost", new { SeName = await blogPost.GetActiveSlugAsync(ensureTwoPublishedLanguages: false) }, protocol);
                var content = blogPost.GetLocalized(x => x.Body, detectEmptyHtml: true).Value;

                if (content.HasValue())
                {
                    content = WebHelper.MakeAllUrlsAbsolute(content, Request);
                }

                var item = feed.CreateItem(
                    blogPost.GetLocalized(x => x.Title),
                    content,
                    blogPostUrl,
                    blogPost.CreatedOnUtc);

                items.Add(item);

                Services.DisplayControl.Announce(blogPost);
            }

            feed.Items = items;

            Services.DisplayControl.AnnounceRange(blogPosts);

            return new RssActionResult(feed);
        }

        [GdprConsent]
        public async Task<IActionResult> BlogPost(int blogPostId)
        {
            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            var blogPost = await _db.BlogPosts()
                .AsSplitQuery()
                .Include(x => x.BlogComments)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .FindByIdAsync(blogPostId, false);

            if (blogPost == null)
            {
                return NotFound();
            }

            if (!blogPost.IsPublished ||
                (blogPost.LanguageId.HasValue && blogPost.LanguageId != _services.WorkContext.WorkingLanguage.Id) ||
                (blogPost.StartDateUtc.HasValue && blogPost.StartDateUtc.Value >= DateTime.UtcNow) ||
                (blogPost.EndDateUtc.HasValue && blogPost.EndDateUtc.Value <= DateTime.UtcNow) ||
                !await _storeMappingService.AuthorizeAsync(blogPost))
            {
                if (!_services.WorkContext.CurrentCustomer.IsAdmin())
                {
                    return NotFound();
                }
            }

            var model = await blogPost.MapAsync(new { PrepareComments = true });

            ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;
            ViewBag.StoreName = _services.StoreContext.CurrentStore.Name;

            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha, ValidateHoneypot]
        [GdprConsent]
        public async Task<IActionResult> BlogCommentAdd(PublicBlogPostModel model, string captchaError)
        {
            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            var blogPost = await _db.BlogPosts().FindByIdAsync(model.Id, false);
            if (blogPost == null || !blogPost.AllowComments)
            {
                return NotFound();
            }

            var customer = _services.WorkContext.CurrentCustomer;
            if (customer.IsGuest() && !_blogSettings.AllowNotRegisteredUsersToLeaveComments)
            {
                ModelState.AddModelError(string.Empty, T("Blog.Comments.OnlyRegisteredUsersLeaveComments"));
            }

            if (_captchaSettings.ShowOnBlogCommentPage && captchaError.HasValue())
            {
                ModelState.AddModelError(string.Empty, captchaError);
            }

            if (ModelState.IsValid)
            {
                var comment = new BlogComment
                {
                    BlogPostId = blogPost.Id,
                    CustomerId = customer.Id,
                    IpAddress = _webHelper.Value.GetClientIpAddress().ToString(),
                    CommentText = HtmlUtility.SanitizeHtml(model.AddNewComment.CommentText, HtmlSanitizerOptions.UserCommentSuitable),
                    IsApproved = true
                };

                _db.CustomerContent.Add(comment);
                await _db.SaveChangesAsync();

                // Notify the store owner.
                if (_blogSettings.NotifyAboutNewBlogComments)
                {
                    await _messageFactory.Value.SendBlogCommentNotificationMessage(comment, _localizationSettings.DefaultAdminLanguageId);
                }

                _activityLogger.Value.LogActivity(KnownActivityLogTypes.PublicStoreAddBlogComment, T("ActivityLog.PublicStore.AddBlogComment"));

                NotifySuccess(T("Blog.Comments.SuccessfullyAdded"));

                var seName = await blogPost.GetActiveSlugAsync(ensureTwoPublishedLanguages: false);
                var url = Url.RouteUrl(new UrlRouteContext
                {
                    RouteName = "BlogPost",
                    Values = new { SeName = seName },
                    Fragment = "customer-comment-list"
                });

                return Redirect(url);
            }

            // If we got this far something failed. Redisplay form.
            model = await blogPost.MapAsync(new { PrepareComments = true });

            ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;
            ViewBag.StoreName = _services.StoreContext.CurrentStore.Name;

            return View(nameof(BlogPost), model);
        }
    }
}
