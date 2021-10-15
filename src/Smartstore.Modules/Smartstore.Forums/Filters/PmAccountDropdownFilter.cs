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
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                if (_forumSettings.AllowPrivateMessages)
                {
                    var widget = new ComponentWidgetInvoker(typeof(PmAccountDropdownViewComponent), null);
                    // TODO: (mg) (core) It also can't be placed topmost, please! Such things shouldn't even be in discussion.
                    // Place it before the sparator (make a new widget zone for it). Actually we should have made a CMS menu
                    // out of this dropdown, but we missed that.
                    _widgetProvider.Value.RegisterWidget(new[] { "account_dropdown_before" }, widget);
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
