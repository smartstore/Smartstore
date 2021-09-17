using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;

namespace Smartstore.Blog.Filters
{
    public class BlogMenuItemFilter : IResultFilter
    {
        private readonly BlogSettings _blogSettings;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ICommonServices _services; // TODO: Use Localization Service
        private readonly Lazy<IUrlHelper> _urlHelper;

        public BlogMenuItemFilter(BlogSettings blogSettings, IWidgetProvider widgetProvider, ICommonServices services, Lazy<IUrlHelper> urlHelper)
        {
            _blogSettings = blogSettings;
            _widgetProvider = widgetProvider;
            _services = services;
            _urlHelper = urlHelper;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_blogSettings.Enabled)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (!filterContext.Result.IsHtmlViewResult())
                return;

            var html = $"<a class='menubar-link' href='{_urlHelper.Value.RouteUrl("Blog")}'>{_services.Localization.GetResource("Blog")}</a>";

            _widgetProvider.RegisterHtml(new[] { "header_menu_special" }, new HtmlString(html));
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
