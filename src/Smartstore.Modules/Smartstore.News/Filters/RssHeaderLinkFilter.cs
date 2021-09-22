using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;

namespace Smartstore.News.Filters
{
    public class RssHeaderLinkFilter : IResultFilter
    {
        private readonly NewsSettings _newsSettings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IStoreContext _storeContext;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWebHelper> _webHelper;
        
        public RssHeaderLinkFilter(
            NewsSettings newsSettings, 
            IWidgetProvider widgetProvider, 
            IStoreContext storeContext, 
            Lazy<IUrlHelper> urlHelper, 
            Lazy<IWebHelper> webHelper)
        {
            _newsSettings = newsSettings;
            _widgetProvider = widgetProvider;
            _storeContext = storeContext;
            _urlHelper = urlHelper;
            _webHelper = webHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_newsSettings.Enabled || !_newsSettings.ShowHeaderRssUrl)
                return;

            // Should only run on a full view rendering result or HTML ContentResult.
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            var url = _urlHelper.Value.RouteUrl("NewsRSS", null, _webHelper.Value.IsCurrentConnectionSecured() ? "https" : "http");
            var link = $"<link href='{url}' rel='alternate' type='application/rss+xml' title='{_storeContext.CurrentStore.Name} - News' />";

            _widgetProvider.RegisterHtml(new[] { "head_links" }, new HtmlString(link));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
