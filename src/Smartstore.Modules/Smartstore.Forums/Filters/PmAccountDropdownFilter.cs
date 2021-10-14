using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;

namespace Smartstore.Forums.Filters
{
    /// <summary>
    /// Frontend account menu item to get to the private message inbox page.
    /// </summary>
    public class PmAccountDropdownFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public PmAccountDropdownFilter(Lazy<IWidgetProvider> widgetProvider, ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // TODO: (mg) (core) PM menu item CANNOT be placed below Signout item!
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                if (_forumSettings.AllowPrivateMessages)
                {
                    var widget = new ComponentWidgetInvoker(typeof(PmAccountDropdownViewComponent), null);

                    _widgetProvider.Value.RegisterWidget(new[] { "account_dropdown_after" }, widget);
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
