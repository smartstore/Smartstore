using Smartstore.Core.Content.Topics;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Infrastructure.Hooks;
using Smartstore.Web.Models.Topics;

namespace Smartstore.Web.Components
{
    public class TopicBlockViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(string systemName, bool bodyOnly = false, bool isLead = false)
        {
            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;

            var cacheKey = string.Format(ModelCacheInvalidator.TOPIC_BY_SYSTEMNAME_KEY,
                systemName.ToLower(),
                Services.WorkContext.WorkingLanguage.Id,
                store.Id,
                customer.GetRolesIdent());

            var cacheModel = await Services.CacheFactory.GetMemoryCache().GetAsync(cacheKey, async () =>
            {
                // INFO: TopicModel contains unserializable properties.
                var topic = await Services.DbContext.Topics
                    .AsNoTracking()
                    .Where(x => x.SystemName == systemName)
                    .ApplyStandardFilter(false, customer.GetRoleIds(), store.Id)
                    .FirstOrDefaultAsync();

                if (topic == null || !topic.IsPublished)
                {
                    return null;
                }

                return PrepareTopicModel(topic);
            });

            if (cacheModel == null)
            {
                return Content(string.Empty);
            }

            ViewBag.BodyOnly = bodyOnly;
            ViewBag.IsLead = isLead;

            if (!cacheModel.RenderAsWidget)
            {
                Services.DisplayControl.Announce(new Topic { Id = cacheModel.Id });
            }

            return View(cacheModel);
        }

        private static TopicModel PrepareTopicModel(Topic topic)
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

            return model;
        }
    }
}
