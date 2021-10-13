using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets;

namespace Smartstore.Forums.Filters
{
    /// <summary>
    /// Backend link on customer edit page to send a PN.
    /// </summary>
    public class PmMessagingDropdownFilter : IResultFilter
    {
        private const int MENU_ITEM_ORDER = 1001;

        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly Lazy<IUrlHelper> _urlHelper;
        private readonly Lazy<ILocalizationService> _localizationService;
        private readonly ForumSettings _forumSettings;

        public PmMessagingDropdownFilter(
            Lazy<IWidgetProvider> widgetProvider,
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
            if (_forumSettings.AllowPrivateMessages && filterContext.Result.IsHtmlViewResult())
            {
                if (filterContext?.RouteData?.Values?.TryGetValue("id", out var customerIdObj) ?? false)
                {
                    var customerId = customerIdObj.ToString().ToInt();
                    if (customerId != 0)
                    {
                        var html = "<a class='dropdown-item send-pm-link' href='{0}'>{1}</a>".FormatInvariant(
                            _urlHelper.Value.Action("SendPm", "Forum", new { id = customerId }),
                            _localizationService.Value.GetResource("Admin.Customers.Customers.SendPM"));

                        _widgetProvider.Value.RegisterHtml(new[] { "admin_button_toolbar_messaging_dropdown" }, new HtmlString(html), MENU_ITEM_ORDER);
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
