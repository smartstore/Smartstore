using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;

namespace Smartstore.DevTools.Filters
{
    public class WidgetZoneFilter : IActionFilter, IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ProfilerSettings _profilerSettings;

        public WidgetZoneFilter(
            ICommonServices services,
            IWidgetProvider widgetProvider,
            ProfilerSettings profilerSettings)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _profilerSettings = profilerSettings;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_profilerSettings.DisplayWidgetZones)
                return;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_profilerSettings.DisplayWidgetZones)
                return;

            // should only run on a full view rendering result or HTML ContentResult
            if (filterContext.Result is StatusCodeResult || filterContext.Result.IsHtmlViewResult())
            {
                if (!ShouldRender(filterContext.HttpContext.Request))
                {
                    return;
                }

                var widget = new ComponentWidgetInvoker("WidgetZone", "Smartstore.DevTools", null);

                // INFO: Don't render in zones where replace-content is true & no <head> zones
                _widgetProvider.RegisterWidget(new Regex(@"^(?!header)(?!footer)(?!stylesheets)(?!head_scripts)(?!head_canonical)(?!head_links)(?!head).*$"), widget);
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        private bool ShouldRender(HttpRequest request)
        {
            if (!_services.WorkContext.CurrentCustomer.IsAdmin())
            {
                if (request.Path == "/common/pdfreceiptfooter" || request.Path == "/common/pdfreceiptheader")
                {
                    return false;
                }
                return request.HttpContext.Connection.IsLocal();
            }

            return true;
        }
    }
}