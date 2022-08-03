using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Widgets;
using Smartstore.DevTools.Components;
using Smartstore.Diagnostics;

namespace Smartstore.DevTools.Filters
{
    internal class MiniProfilerFilter : IActionFilter, IResultFilter
    {

        private readonly IChronometer _chronometer;
        private readonly IWidgetProvider _widgetProvider;

        public MiniProfilerFilter(IChronometer chronometer, IWidgetProvider widgetProvider)
        {
            _chronometer = chronometer;
            _widgetProvider = widgetProvider;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var routeIdent = context.RouteData.Values.GenerateRouteIdentifier();
            _chronometer.StepStart("ActionFilter", "Action: " + routeIdent);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (!context.Result.IsHtmlViewResult())
            {
                _chronometer.StepStop("ActionFilter");
            }
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (!context.Result.IsHtmlViewResult())
            {
                return;
            }

            var viewResult = context.Result is IActionResultContainer container
                ? container.InnerResult as ViewResult
                : context.Result as ViewResult;

            var viewName = viewResult?.ViewName;
            if (viewName.IsEmpty())
            {
                var action = context.RouteData.Values.GetActionName().EmptyNull();
                viewName = action;
            }

            _chronometer.StepStart("ResultFilter", $"View: {viewName}");

            _widgetProvider.RegisterViewComponent<MiniProfilerViewComponent>("head");
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            if (context.Result.IsHtmlViewResult())
            {
                _chronometer.StepStop("ResultFilter");
                _chronometer.StepStop("ActionFilter");
            }
        }
    }
}
