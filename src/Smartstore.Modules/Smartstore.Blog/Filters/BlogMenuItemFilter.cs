using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Blog.Filters
{
    public class BlogMenuItemFilter : IResultFilter
    {
        private readonly BlogSettings _blogSettings;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public BlogMenuItemFilter(
            BlogSettings blogSettings,
            Lazy<IWidgetProvider> widgetProvider,
            Lazy<ILocalizationService> localizationService,
            Lazy<IUrlHelper> urlHelper)
        {
            _blogSettings = blogSettings;
            _widgetProvider = widgetProvider;
            _localizationService = localizationService;
            _urlHelper = urlHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_blogSettings.Enabled)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                var html = $"<a class='menubar-link' href='{_urlHelper.Value.RouteUrl("Blog")}'>{_localizationService.Value.GetResource("Blog")}</a>";

                _widgetProvider.Value.RegisterHtml(new[] { "header_menu_special" }, new HtmlString(html));
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
