using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Blog.Messaging;
using Smartstore.Blog.Models.Public;
using Smartstore.Caching.OutputCache;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Http;
using Smartstore.Net;
using Smartstore.Web.Controllers;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;
using Smartstore.Web.Models.Media;

namespace Smartstore.Blog.Controllers
{
    public class BlogController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IMediaService _mediaService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IPageAssetBuilder _pageAssetBuilder;
        private readonly Lazy<IWebHelper> _webHelper;
        private readonly Lazy<IActivityLogger> _activityLogger; 
        private readonly Lazy<IMessageFactory> _messageFactory;
        private readonly Lazy<LinkGenerator> _linkGenerator;

        private readonly BlogSettings _blogSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly SeoSettings _seoSettings;

        public BlogController(
            SmartDbContext db,
            ICommonServices services,
            IMediaService mediaService,
            IDateTimeHelper dateTimeHelper,
            IStoreMappingService storeMappingService,
            IPageAssetBuilder pageAssetBuilder,
            Lazy<IWebHelper> webHelper,
            Lazy<IActivityLogger> activityLogger,
            Lazy<IMessageFactory> messageFactory,
            Lazy<LinkGenerator> linkGenerator,
            BlogSettings blogSettings,
            LocalizationSettings localizationSettings,
            CustomerSettings customerSettings,
            CaptchaSettings captchaSettings,
            SeoSettings seoSettings)
        {
            _db = db;
            _services = services;
            _mediaService = mediaService;
            _dateTimeHelper = dateTimeHelper;
            _storeMappingService = storeMappingService;
            _pageAssetBuilder = pageAssetBuilder;
            _webHelper = webHelper;
            _activityLogger = activityLogger;
            _messageFactory = messageFactory;
            _linkGenerator = linkGenerator;

            _blogSettings = blogSettings;
            _localizationSettings = localizationSettings;
            _customerSettings = customerSettings;
            _captchaSettings = captchaSettings;
            _seoSettings = seoSettings;
        }

        #region Utilities

        [NonAction]
        protected async Task<ImageModel> PrepareBlogPostPictureModelAsync(BlogPost blogPost, int? fileId)
        {
            var file = await _mediaService.GetFileByIdAsync(fileId ?? 0, MediaLoadFlags.AsNoTracking);

            var pictureModel = new ImageModel
            {
                File = file,
                ThumbSize = MediaSettings.ThumbnailSizeLg,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? blogPost.GetLocalized(x => x.Title),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? blogPost.GetLocalized(x => x.Title),
            };

            _services.DisplayControl.Announce(file?.File);

            return pictureModel;
        }

        [NonAction]
        protected async Task PrepareBlogPostModelAsync(PublicBlogPostModel model, BlogPost blogPost, bool prepareComments)
        {
            Guard.NotNull(blogPost, nameof(blogPost));
            Guard.NotNull(model, nameof(model));

            MiniMapper.Map(blogPost, model);

            model.Title = blogPost.GetLocalized(x => x.Title);
            model.Intro = blogPost.GetLocalized(x => x.Intro);
            model.Body = blogPost.GetLocalized(x => x.Body, true);
            model.MetaTitle = blogPost.GetLocalized(x => x.MetaTitle);
            model.MetaDescription = blogPost.GetLocalized(x => x.MetaDescription);
            model.MetaKeywords = blogPost.GetLocalized(x => x.MetaKeywords);
            model.SeName = await blogPost.GetActiveSlugAsync(ensureTwoPublishedLanguages: false);
            model.CreatedOn = _dateTimeHelper.ConvertToUserTime(blogPost.CreatedOnUtc, DateTimeKind.Utc);
            model.CreatedOnUTC = blogPost.CreatedOnUtc;
            model.AddNewComment.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnBlogCommentPage;
            model.Comments.AllowComments = blogPost.AllowComments;
            model.Comments.NumberOfComments = blogPost.ApprovedCommentCount;
            model.Comments.AllowCustomersToUploadAvatars = _customerSettings.AllowCustomersToUploadAvatars;
            model.DisplayAdminLink = _services.Permissions.Authorize(Permissions.System.AccessBackend, _services.WorkContext.CurrentCustomer);

            model.HasBgImage = blogPost.PreviewDisplayType == PreviewDisplayType.DefaultSectionBg || blogPost.PreviewDisplayType == PreviewDisplayType.PreviewSectionBg;

            model.Image = await PrepareBlogPostPictureModelAsync(blogPost, blogPost.MediaFileId);

            if (blogPost.PreviewDisplayType == PreviewDisplayType.Default || blogPost.PreviewDisplayType == PreviewDisplayType.DefaultSectionBg)
            {
                model.Preview = await PrepareBlogPostPictureModelAsync(blogPost, blogPost.MediaFileId);
            }
            else if (blogPost.PreviewDisplayType == PreviewDisplayType.Preview || blogPost.PreviewDisplayType == PreviewDisplayType.PreviewSectionBg)
            {
                model.Preview = await PrepareBlogPostPictureModelAsync(blogPost, blogPost.PreviewMediaFileId);
            }

            if (blogPost.PreviewDisplayType == PreviewDisplayType.Preview ||
                blogPost.PreviewDisplayType == PreviewDisplayType.Default ||
                blogPost.PreviewDisplayType == PreviewDisplayType.Bare)
            {
                model.SectionBg = string.Empty;
            }

            model.Tags = blogPost.ParseTags().Select(x => new BlogPostTagModel
            {
                Name = x,
                SeName = SeoHelper.BuildSlug(x)
            }).ToList();

            if (prepareComments)
            {
                var blogComments = blogPost.BlogComments
                    .Where(pr => pr.IsApproved)
                    .OrderBy(pr => pr.CreatedOnUtc);

                foreach (var bc in blogComments)
                {
                    var isGuest = bc.Customer.IsGuest();

                    var commentModel = new CommentModel(model.Comments)
                    {
                        Id = bc.Id,
                        CustomerId = bc.CustomerId,
                        CustomerName = bc.Customer.FormatUserName(_customerSettings, T, false),
                        CommentText = bc.CommentText,
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(bc.CreatedOnUtc, DateTimeKind.Utc),
                        CreatedOnPretty = _services.DateTimeHelper.ConvertToUserTime(bc.CreatedOnUtc, DateTimeKind.Utc).Humanize(false),
                        AllowViewingProfiles = _customerSettings.AllowViewingProfiles && !isGuest
                    };

                    commentModel.Avatar = bc.Customer.ToAvatarModel(null, false);

                    model.Comments.Comments.Add(commentModel);
                }
            }

            ViewBag.CanonicalUrlsEnabled = _seoSettings.CanonicalUrlsEnabled;
            ViewBag.StoreName = _services.StoreContext.CurrentStore.Name;

            Services.DisplayControl.Announce(blogPost);
        }

        [NonAction]
        protected async Task<BlogPostListModel> PrepareBlogPostListModelAsync(BlogPagingFilteringModel command)
        {
            Guard.NotNull(command, nameof(command));

            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel();
            model.PagingFilteringContext.Tag = command.Tag;
            model.PagingFilteringContext.Month = command.Month;

            if (command.PageSize <= 0)
                command.PageSize = _blogSettings.PostsPageSize;
            if (command.PageNumber <= 0)
                command.PageNumber = 1;

            DateTime? dateFrom = command.GetFromMonth();
            DateTime? dateTo = command.GetToMonth();

            var query = _db.BlogPosts().AsNoTracking().ApplyStandardFilter(storeId, languageId, isAdmin).AsQueryable();

            if (!command.Tag.HasValue())
            {
                query = query.ApplyTimeFilter(dateFrom, dateTo);
            }
            
            var blogPosts = command.Tag.HasValue() 
                ? (await query.ToListAsync())
                    .FilterByTag(command.Tag)
                    .ToPagedList(command.PageNumber - 1, command.PageSize)
                : query.ToPagedList(command.PageNumber - 1, command.PageSize);

            var pagedBlogPosts = await blogPosts.LoadAsync();

            model.PagingFilteringContext.LoadPagedList(pagedBlogPosts);

            // Prepare SEO model.
            var parsedMonth = model.PagingFilteringContext.GetParsedMonth();
            var tag = model.PagingFilteringContext.Tag;

            if (parsedMonth == null && tag == null)
            {
                model.MetaTitle = _blogSettings.GetLocalizedSetting(x => x.MetaTitle, storeId);
                model.MetaDescription = _blogSettings.GetLocalizedSetting(x => x.MetaDescription, storeId);
                model.MetaKeywords = _blogSettings.GetLocalizedSetting(x => x.MetaKeywords, storeId);
            }
            else
            {
                model.MetaTitle = parsedMonth != null ?
                    T("PageTitle.Blog.Month", parsedMonth.Value.ToNativeString("MMMM", CultureInfo.InvariantCulture) + " " + parsedMonth.Value.Year) :
                    T("PageTitle.Blog.Tag", tag);

                model.MetaDescription = parsedMonth != null ?
                    T("Metadesc.Blog.Month", parsedMonth.Value.ToNativeString("MMMM", CultureInfo.InvariantCulture) + " " + parsedMonth.Value.Year) :
                    T("Metadesc.Blog.Tag", tag);

                model.MetaKeywords = parsedMonth != null ? parsedMonth.Value.ToNativeString("MMMM", CultureInfo.InvariantCulture) + " " + parsedMonth.Value.Year : tag;
            }

            model.StoreName = _services.StoreContext.CurrentStore.Name;

            Services.DisplayControl.AnnounceRange(pagedBlogPosts);

            model.BlogPosts = await pagedBlogPosts
                .SelectAsync(async x =>
                {
                    var blogPostModel = new PublicBlogPostModel();
                    await PrepareBlogPostModelAsync(blogPostModel, x, false);
                    return blogPostModel;
                })
                .AsyncToList();

            return model;
        }

        [NonAction]
        protected async Task<BlogPostListModel> PrepareBlogPostListModelAsync(
            int? maxPostAmount, 
            int? maxAgeInDays, 
            bool renderHeading, 
            string blogHeading, 
            bool disableCommentCount, 
            string postsWithTag)
        {
            var storeId = _services.StoreContext.CurrentStore.Id;
            var languageId = _services.WorkContext.WorkingLanguage.Id;
            var isAdmin = _services.WorkContext.CurrentCustomer.IsAdmin();

            var model = new BlogPostListModel
            {
                BlogHeading = blogHeading,
                RenderHeading = renderHeading,
                RssToLinkButton = renderHeading,
                DisableCommentCount = disableCommentCount
            };

            DateTime? maxAge = null;
            if (maxAgeInDays.HasValue)
            {
                maxAge = DateTime.UtcNow.AddDays(-maxAgeInDays.Value);
            }

            var query = _db.BlogPosts()
                .AsNoTracking()
                .ApplyStandardFilter(storeId, languageId, isAdmin)
                .ApplyTimeFilter(maxAge: maxAge)
                .AsQueryable();

            var blogPosts = await query.ToListAsync();

            if (!postsWithTag.IsEmpty())
            {
                blogPosts = blogPosts.FilterByTag(postsWithTag).ToList();
            }

            var pagedBlogPosts = await blogPosts
                .ToPagedList(0, maxPostAmount ?? 100)
                .LoadAsync();

            Services.DisplayControl.AnnounceRange(blogPosts);

            model.BlogPosts = await blogPosts
                .SelectAsync(async x =>
                {
                    var blogPostModel = new PublicBlogPostModel();
                    await PrepareBlogPostModelAsync(blogPostModel, x, false);
                    return blogPostModel;
                })
                .AsyncToList();

            return model;
        }

        #endregion

        #region Methods 

        [LocalizedRoute("blog", Name = "Blog")]
        public async Task<IActionResult> List(BlogPagingFilteringModel command)
        {
            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            var model = await PrepareBlogPostListModelAsync(command);

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                var url = Url.RouteUrl("Blog", null, Request.Scheme);
                _pageAssetBuilder.AppendCanonicalUrlParts(url);
            }

            return View("List", model);
        }

        public async Task<IActionResult> BlogSummary(
            int? maxPostAmount, 
            int? maxAgeInDays, 
            bool renderHeading, 
            string blogHeading, 
            bool disableCommentCount, 
            string postsWithTag)
        {
            var model = await PrepareBlogPostListModelAsync(maxPostAmount, maxAgeInDays, renderHeading, blogHeading, disableCommentCount, postsWithTag);

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

            var model = await PrepareBlogPostListModelAsync(command);
            return View("List", model);
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

            var model = await PrepareBlogPostListModelAsync(command);
            return View("List", model);
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
            feed.Init(selfLink, language.LanguageCulture);

            if (!_blogSettings.Enabled)
            {
                return new RssActionResult { Feed = feed };
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

            return new RssActionResult { Feed = feed };
        }

        [GdprConsent]
        public async Task<IActionResult> BlogPost(int blogPostId)
        {
            if (!_blogSettings.Enabled)
            {
                return NotFound();
            }

            var blogPost = await _db.BlogPosts()
                .Include(x => x.BlogComments)
                .ThenInclude(x => x.Customer)
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

            var model = new PublicBlogPostModel();
            await PrepareBlogPostModelAsync(model, blogPost, true);

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
                ModelState.AddModelError("", T("Blog.Comments.OnlyRegisteredUsersLeaveComments"));
            }

            if (_captchaSettings.ShowOnBlogCommentPage && captchaError.HasValue())
            {
                ModelState.AddModelError("", captchaError);
            }

            if (ModelState.IsValid)
            {
                var comment = new BlogComment
                {
                    BlogPostId = blogPost.Id,
                    CustomerId = customer.Id,
                    IpAddress = _webHelper.Value.GetClientIpAddress().ToString(),
                    CommentText = model.AddNewComment.CommentText,
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
                var url = _linkGenerator.Value.GetPathByRouteValues("BlogPost", new { SeName = seName }, fragment: new FragmentString("#customer-comment-list"));
                return Redirect(url);
            }

            // If we got this far something failed. Redisplay form.
            await PrepareBlogPostModelAsync(model, blogPost, true);
            return View("BlogPost", model);
        }

        #endregion
    }
}
