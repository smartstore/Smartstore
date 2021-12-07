using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;

namespace Smartstore.Forums.Filters
{
    /// <summary>
    /// Backend link on customer edit page to send a PM.
    /// </summary>
    public class PmMessagingDropdownFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public PmMessagingDropdownFilter(Lazy<IWidgetProvider> widgetProvider, ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
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
                        var widget = new ComponentWidgetInvoker(typeof(AdminPmButtonViewComponent), new { customerId });
                        _widgetProvider.Value.RegisterWidget(new[] { "admin_button_toolbar_messaging_dropdown" }, widget);
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
