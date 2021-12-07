using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Stores;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;

namespace Smartstore.Blog.Filters
{
    public class RssHeaderLinkFilter : IResultFilter
    {
        private readonly BlogSettings _blogSettings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IStoreContext _storeContext;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<IWebHelper> _webHelper;

        public RssHeaderLinkFilter(BlogSettings blogSettings, IWidgetProvider widgetProvider, IStoreContext storeContext, Lazy<IUrlHelper> urlHelper, Lazy<IWebHelper> webHelper)
        {
            _blogSettings = blogSettings;
            _widgetProvider = widgetProvider;
            _storeContext = storeContext;
            _urlHelper = urlHelper;
            _webHelper = webHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_blogSettings.Enabled || !_blogSettings.ShowHeaderRssUrl)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            var url = _urlHelper.Value.RouteUrl("BlogRSS", null, _webHelper.Value.IsCurrentConnectionSecured() ? "https" : "http");
            var link = $"<link href='{url}' rel='alternate' type='application/rss+xml' title='{_storeContext.CurrentStore.Name} - Blog' />";

            _widgetProvider.RegisterHtml(new[] { "head_links" }, new HtmlString(link));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
