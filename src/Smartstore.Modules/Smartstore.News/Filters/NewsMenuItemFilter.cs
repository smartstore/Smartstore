using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.News.Filters
{
    public class NewsMenuItemFilter : IResultFilter
    {
        private readonly NewsSettings _newsSettings;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly Lazy<IUrlHelper> _urlHelper;

        public NewsMenuItemFilter(
            NewsSettings newsSettings,
            Lazy<IWidgetProvider> widgetProvider,
            Lazy<ILocalizationService> localizationService, 
            Lazy<IUrlHelper> urlHelper)
        {
            _newsSettings = newsSettings;
            _widgetProvider = widgetProvider;
            _localizationService = localizationService;
            _urlHelper = urlHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_newsSettings.Enabled)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            var html = $"<a class='menubar-link' href='{_urlHelper.Value.RouteUrl("NewsArchive")}'>{_localizationService.Value.GetResource("News")}</a>";

            _widgetProvider.Value.RegisterHtml(new[] { "header_menu_special" }, new HtmlString(html));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
