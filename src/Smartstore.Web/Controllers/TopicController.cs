using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Topics;

namespace Smartstore.Web.Controllers
{
    public class TopicController : PublicController
    {
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly SeoSettings _seoSettings;
        private readonly IBreadcrumb _breadcrumb;
        private readonly CatalogHelper _helper;
        //private readonly ICookieManager _cookieManager;

        public TopicController(
            IAclService aclService,
            IStoreMappingService storeMappingService,
            SeoSettings seoSettings,
            IBreadcrumb breadcrumb,
            CatalogHelper helper)
        {
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _seoSettings = seoSettings;
            _breadcrumb = breadcrumb;
            _helper = helper;
        }

        protected TopicModel PrepareTopicModel(Topic topic)
        {
            Guard.NotNull(topic, nameof(topic));

            var titleTag = "h3";
            if (topic.TitleTag != null)
            {
                titleTag = topic.TitleTag;
            }
            else if (!topic.RenderAsWidget)
            {
                titleTag = "h1";
            }

            var model = new TopicModel
            {
                Id = topic.Id,
                SystemName = topic.SystemName,
                HtmlId = topic.HtmlId,
                BodyCssClass = topic.BodyCssClass,
                IsPasswordProtected = topic.IsPasswordProtected,
                ShortTitle = topic.IsPasswordProtected ? null : topic.GetLocalized(x => x.ShortTitle),
                Title = topic.IsPasswordProtected ? null : topic.GetLocalized(x => x.Title),
                Intro = topic.IsPasswordProtected ? null : topic.GetLocalized(x => x.Intro),
                Body = topic.IsPasswordProtected ? null : topic.GetLocalized(x => x.Body, detectEmptyHtml: true),
                MetaKeywords = topic.GetLocalized(x => x.MetaKeywords),
                MetaDescription = topic.GetLocalized(x => x.MetaDescription),
                MetaTitle = topic.GetLocalized(x => x.MetaTitle),
                SeName = topic.GetActiveSlug(),
                TitleTag = titleTag,
                RenderAsWidget = topic.RenderAsWidget
            };

            if (_seoSettings.CanonicalUrlsEnabled)
            {
                model.CanonicalUrl = Url.RouteUrl("Topic", new { model.SeName }, Request.Scheme);
            }

            return model;
        }

        public async Task<IActionResult> TopicDetails(int topicId, bool popup = false)
        {
            await _helper.GetBreadcrumbAsync(_breadcrumb, ControllerContext);

            var cacheKey = string.Format(ModelCacheInvalidator.TOPIC_BY_ID_KEY,
                topicId,
                Services.WorkContext.WorkingLanguage.Id,
                Services.StoreContext.CurrentStore.Id,
                Services.WorkContext.CurrentCustomer.GetRolesIdent());

            var cacheModel = await Services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var topic = await Services.DbContext.Topics.FindByIdAsync(topicId, false);
                if (topic == null || !topic.IsPublished)
                    return null;

                if (!await _storeMappingService.AuthorizeAsync(topic))
                    return null;

                if (!await _aclService.AuthorizeAsync(topic))
                    return null;

                return PrepareTopicModel(topic);
            });

            if (cacheModel == null || (!popup && cacheModel.RenderAsWidget))
                return NotFound();

            ViewBag.IsPopup = popup;

            if (!cacheModel.RenderAsWidget)
            {
                Services.DisplayControl.Announce(new Topic { Id = cacheModel.Id });
            }

            return View("TopicDetails", cacheModel);
        }

        [HttpPost]
        public async Task<IActionResult> Authenticate(int id, string password)
        {
            var authResult = false;
            var title = string.Empty;
            var body = string.Empty;
            var error = string.Empty;
            var db = Services.DbContext;

            var topic = await db.Topics.FindByIdAsync(id, false);

            if (topic != null &&
                topic.IsPublished &&
                topic.IsPasswordProtected &&
                await _storeMappingService.AuthorizeAsync(topic) &&
                await _aclService.AuthorizeAsync(topic))
            {
                if (topic.Password != null && topic.Password.Equals(password))
                {
                    authResult = true;
                    title = topic.GetLocalized(x => x.Title).Value;
                    body = topic.GetLocalized(x => x.Body, detectEmptyHtml: true).Value;
                }
                else
                {
                    error = T("Topic.WrongPassword").Value;
                }
            }

            return Json(new { Authenticated = authResult, Title = title, Body = body, Error = error });
        }
    }
}
