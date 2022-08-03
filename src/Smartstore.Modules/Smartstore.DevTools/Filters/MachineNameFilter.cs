using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using Smartstore.DevTools.Components;
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
            {
                return;
            }

            if (!filterContext.HttpContext.Request.IsNonAjaxGet())
            {
                return;
            }

            // should only run on a full view rendering result or HTML ContentResult
            if (!filterContext.Result.IsHtmlViewResult())
            {
                return;
            }

            _widgetProvider.RegisterViewComponent<MachineNameViewComponent>("end");
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }
    }
}
