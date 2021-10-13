using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;

namespace Smartstore.Forums.Filters
{
    public class CustomerProfileFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public CustomerProfileFilter(Lazy<IWidgetProvider> widgetProvider, ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext.Result.IsHtmlViewResult())
            {
                if (filterContext?.RouteData?.Values?.TryGetValue("id", out var customerIdObj) ?? false)
                {
                    var customerId = customerIdObj.ToString().ToInt();
                    if (customerId != 0)
                    {
                        // PM button.
                        if (_forumSettings.AllowPrivateMessages)
                        {
                            var widget = new ComponentWidgetInvoker(typeof(PmButtonViewComponent), customerId);

                            _widgetProvider.Value.RegisterWidget(new[] { "profile_page_info_userdetails" }, widget);
                        }

                        // Forum statistics.
                        if (_forumSettings.ForumsEnabled && _forumSettings.ShowCustomersPostCount)
                        {
                            var widget = new ComponentWidgetInvoker(typeof(ForumCustomerStatsViewComponent), customerId);

                            _widgetProvider.Value.RegisterWidget(new[] { "profile_page_info_userstats" }, widget);
                        }
                    }
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
