using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;

namespace Smartstore.Blog.Filters
{
    public class RssHeaderLinkFilter : IResultFilter
    {
        private readonly BlogSettings _blogSettings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWebHelper> _webHelper;
        
        public RssHeaderLinkFilter(BlogSettings blogSettings, IWidgetProvider widgetProvider, ICommonServices services, Lazy<IUrlHelper> urlHelper, Lazy<IWebHelper> webHelper)
        {
            _blogSettings = blogSettings;
            _widgetProvider = widgetProvider;
            _services = services;
            _urlHelper = urlHelper;
            _webHelper = webHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_blogSettings.Enabled || !_blogSettings.ShowHeaderRssUrl)
                return;

            var result = filterContext.Result;

            // should only run on a full view rendering result or HTML ContentResult
            if (!result.IsHtmlViewResult())
                return;

            var url = _urlHelper.Value.RouteUrl("BlogRSS", null, _webHelper.Value.IsCurrentConnectionSecured() ? "https" : "http");
            var link = $"<link href='{url}' rel='alternate' type='application/rss+xml' title='{_services.StoreContext.CurrentStore.Name} - Blog' />";

            _widgetProvider.RegisterHtml(new[] { "head_links" }, new HtmlString(link));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
