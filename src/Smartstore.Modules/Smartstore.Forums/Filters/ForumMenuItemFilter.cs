using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Forums.Filters
{
    public class ForumMenuItemFilter : IResultFilter
    {
        private const int MENU_ITEM_ORDER = 1001;

        private readonly IWidgetProvider _widgetProvider;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly ForumSettings _forumSettings;

        public ForumMenuItemFilter(
            IWidgetProvider widgetProvider,
            Lazy<IUrlHelper> urlHelper,
            Lazy<ILocalizationService> localizationService,
            ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
            _urlHelper = urlHelper;
            _localizationService = localizationService;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (_forumSettings.ForumsEnabled && filterContext.Result.IsHtmlViewResult())
            {
                var html = "<a class='menubar-link' href='{0}'>{1}</a>".FormatInvariant(
                    _urlHelper.Value.RouteUrl("Boards"),
                    _localizationService.Value.GetResource("Forum.Forums"));

                _widgetProvider.RegisterHtml(new[] { "header_menu_special" }, new HtmlString(html), MENU_ITEM_ORDER);
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
