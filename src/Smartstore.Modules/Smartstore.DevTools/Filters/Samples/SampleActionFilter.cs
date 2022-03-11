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
            filterContext.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName);
            filterContext.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName);

            Debug.WriteLine($"Executing: {controllerName} - {actionName}");

            _notifier.Information("Yeah, my plugin action filter works. NICE!");
            // Do something meaningful here ;-)
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
            filterContext.ActionDescriptor.RouteValues.TryGetValue("controller", out var controllerName);
            filterContext.ActionDescriptor.RouteValues.TryGetValue("action", out var actionName);

            Debug.WriteLine($"Executed: {controllerName} - {actionName}");
        }
    }
}