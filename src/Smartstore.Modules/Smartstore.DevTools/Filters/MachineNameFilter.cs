using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.Engine;

namespace Smartstore.DevTools.Filters
{
    public class MachineNameFilter : IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private readonly ProfilerSettings _profilerSettings;
        private readonly IApplicationContext _appContext;

        public MachineNameFilter(
            ICommonServices services,
            IWidgetProvider widgetProvider,
            ProfilerSettings profilerSettings,
            IApplicationContext appContext)
        {
            _services = services;
            _widgetProvider = widgetProvider;
            _profilerSettings = profilerSettings;
            _appContext = appContext;
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (!_profilerSettings.DisplayMachineName)
                return;

            var result = filterContext.Result;

            // should only run on a full view rendering result or HTML ContentResult
            if (!result.IsHtmlViewResult())
                return;

            if (!_services.WorkContext.CurrentCustomer.IsAdmin() && !filterContext.HttpContext.Connection.IsLocal())
            {
                return;
            }
            
            var css = @"<style>
	            .devtools-machinename {
		            position: fixed;
		            right: 0;
		            bottom: 0;
		            z-index: 999999;
		            background: #333;
		            color: #fff;
		            font-size: 0.9em;
		            font-weight: 600;
		            padding: 0.3em 1em;
		            opacity: 0.92;
		            border: 1px solid #fff;
		            border-top-left-radius: 3px;
	            }
            </style>";

            var html = $"<div class='devtools-machinename'>{_appContext.EnvironmentIdentifier}</div>";

            _widgetProvider.RegisterHtml(
                new[] { "body_end_html_tag_before", "admin_content_after" },
                new HtmlString(css + html)
             );
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
