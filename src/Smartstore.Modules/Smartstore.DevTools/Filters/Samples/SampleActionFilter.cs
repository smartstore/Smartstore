using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Logging;

namespace Smartstore.DevTools.Filters.Samples
{
    internal class SampleActionFilter : IActionFilter
    {
        private readonly INotifier _notifier;

        public SampleActionFilter(INotifier notifier)
        {
            _notifier = notifier;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controllerName = filterContext.RouteData.Values.GetControllerName();
            var actionName = filterContext.RouteData.Values.GetActionName();
            Debug.WriteLine($"Executing: {controllerName} - {actionName}");

            _notifier.Information("Yeah, my plugin action filter works. NICE!");
            // Do something meaningful here ;-)
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var controllerName = filterContext.RouteData.Values.GetControllerName();
            var actionName = filterContext.RouteData.Values.GetActionName();

            Debug.WriteLine($"Executed: {controllerName} - {actionName}");
        }
    }
}