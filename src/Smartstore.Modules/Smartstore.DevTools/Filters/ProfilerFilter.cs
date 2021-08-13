using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core;
using Smartstore.Core.Widgets;
using StackExchange.Profiling;

namespace Smartstore.DevTools.Filters
{
    public class ProfilerFilter : IActionFilter, IResultFilter
    {
        private readonly ICommonServices _services;
        private readonly IWidgetProvider _widgetProvider;
        private bool? _shouldProfile;

        public ProfilerFilter(
            ICommonServices services, 
            IWidgetProvider widgetProvider)
        {
            _services = services;
            _widgetProvider = widgetProvider;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!ShouldProfile(context))
                return;

            var routeIdent = context.RouteData.Values.GenerateRouteIdentifier();
            _services.Chronometer.StepStart("ActionFilter", "Action: " + routeIdent);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (!ShouldProfile(context))
                return;

            if (!context.Result.IsHtmlViewResult())
                _services.Chronometer.StepStop("ActionFilter");
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (!ShouldProfile(context))
                return;

            // Should only run on a full view rendering result
            if (!context.Result.IsHtmlViewResult())
                return;

            var viewResult = context.Result as ViewResult;

            var viewName = viewResult?.ViewName;
            if (viewName.IsEmpty())
            {
                var action = (context.RouteData.Values.GetActionName()).EmptyNull();
                viewName = action;
            }

            _services.Chronometer.StepStart("ResultFilter", $"View: {viewName}");

            _widgetProvider.RegisterHtml("head",
                MiniProfiler.Current.RenderIncludes(context.HttpContext, null));
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            if (!ShouldProfile(context))
                return;

            // Should only run on a full view rendering result
            if (!context.Result.IsHtmlViewResult())
                return;

            _services.Chronometer.StepStop("ResultFilter");
            _services.Chronometer.StepStop("ActionFilter");
        }

        private bool ShouldProfile(FilterContext context)
            => _shouldProfile ??= Startup.ShouldProfile(context.HttpContext.Request);
    }
}
