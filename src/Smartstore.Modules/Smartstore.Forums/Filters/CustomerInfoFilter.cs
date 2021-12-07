using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;

namespace Smartstore.Forums.Filters
{
    public class CustomerInfoFilter : IResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public CustomerInfoFilter(Lazy<IWidgetProvider> widgetProvider, ForumSettings forumSettings)
        {
            _widgetProvider = widgetProvider;
            _forumSettings = forumSettings;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // Forum signature.
            if (_forumSettings.ForumsEnabled && _forumSettings.SignaturesEnabled && filterContext.Result.IsHtmlViewResult())
            {
                var widget = new ComponentWidgetInvoker(typeof(ForumCustomerInfoViewComponent), null);

                _widgetProvider.Value.RegisterWidget(new[] { "customer_info_bottom" }, widget);
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
