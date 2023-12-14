using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Topics;

namespace Smartstore.Web.Controllers
{
    public class TopicController : PublicController
    {
        private readonly SmartDbContext _db;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly SeoSettings _seoSettings;
        private readonly IBreadcrumb _breadcrumb;
        private readonly CatalogHelper _helper;

        public TopicController(
            SmartDbContext db,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            SeoSettings seoSettings,
            IBreadcrumb breadcrumb,
            CatalogHelper helper)
        {
            _db = db;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _seoSettings = seoSettings;
            _breadcrumb = breadcrumb;
            _helper = helper;
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

                var topic = await _db.Topics.FindByIdAsync(topicId, false);
                if (topic != null
                    && topic.IsPublished
                    && await _storeMappingService.AuthorizeAsync(topic)
                    && await _aclService.AuthorizeAsync(topic))
                {
                    return PrepareTopicModel(topic, !topic.IsPasswordProtected);
                }

                return null;
            });

            if (cacheModel == null || (!popup && cacheModel.RenderAsWidget))
            {
                return NotFound();
            }

            ViewBag.IsPopup = popup;

            if (!cacheModel.RenderAsWidget)
            {
                Services.DisplayControl.Announce(new Topic { Id = cacheModel.Id });
            }

            return View(cacheModel);
        }

        [HttpPost, DisallowRobot]
        public async Task<IActionResult> Authenticate(int id, string password)
        {
            var success = false;
            var message = string.Empty;
            var content = string.Empty;
            var topic = await _db.Topics.FindByIdAsync(id, false);

            if (topic != null 
                && topic.IsPublished
                && topic.IsPasswordProtected
                && await _storeMappingService.AuthorizeAsync(topic)
                && await _aclService.AuthorizeAsync(topic))
            {
                if (topic.Password != null && password != null && password == topic.Password)
                {
                    var model = PrepareTopicModel(topic, true);
                    content = await InvokePartialViewAsync("_Topic", model);
                    success = true;
                }
                else
                {
                    message = T("Topic.WrongPassword");
                }
            }
            else
            {
                message = T("Common.PageNotFound");
            }

            return Json(new { success, message, content });
        }

        protected TopicModel PrepareTopicModel(Topic topic, bool includePasswordProtectedFields)
        {
            Guard.NotNull(topic);

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
                ShortTitle = includePasswordProtectedFields ? topic.GetLocalized(x => x.ShortTitle) : null,
                Title = includePasswordProtectedFields ? topic.GetLocalized(x => x.Title) : null,
                Intro = includePasswordProtectedFields ? topic.GetLocalized(x => x.Intro) : null,
                Body = includePasswordProtectedFields ? topic.GetLocalized(x => x.Body, detectEmptyHtml: true): null,
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
    }
}
