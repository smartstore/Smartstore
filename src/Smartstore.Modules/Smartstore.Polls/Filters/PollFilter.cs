using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.Polls.Components;

namespace Smartstore.Polls.Filters
{
    public class PollFilter : IAsyncResultFilter
    {
        private readonly Lazy<IWidgetProvider> _widgetProvider;

        public PollFilter(Lazy<IWidgetProvider> widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext filterContext, ResultExecutionDelegate next)
        {
            // Should only run on a full view rendering result or HTML ContentResult.
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                _widgetProvider.Value.RegisterWidget(new[] { "myaccount_menu_after" },
                    new ComponentWidgetInvoker(typeof(PollBlockViewComponent), new { systemKeyword = "MyAccountMenu" }));

                if (filterContext.RouteData.Values.GetControllerName() == "Blog")
                {
                    _widgetProvider.Value.RegisterWidget(new[] { "blog_right_bottom" },
                        new ComponentWidgetInvoker(typeof(PollBlockViewComponent), new { systemKeyword = "Blog" }));
                }
            }

            await next();
        }
    }
}
