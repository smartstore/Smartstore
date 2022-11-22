#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Smartstore.Utilities;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidgetInvoker : WidgetInvoker<PartialViewWidget>
    {
        private const string ActionNameKey = "action";

        private readonly ICompositeViewEngine _viewEngine;

        public PartialViewWidgetInvoker(ICompositeViewEngine viewEngine)
        {
            _viewEngine = viewEngine;
        }

        public override async Task<IHtmlContent> InvokeAsync(WidgetContext context, PartialViewWidget widget)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(widget, nameof(widget));

            // We have to bring forward writer and view context stuff because we need
            // a properly prepared RouteData for view resolution.
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using var writer = new StringWriter(sb);

            if (widget.Model != null)
            {
                context.Model = widget.Model;
            }

            var viewContext = CreateViewContext(context, writer, widget.Model, widget.Module);

            var viewEngineResult = FindView(viewContext, widget.ViewName, widget.IsMainPage);
            viewEngineResult.EnsureSuccessful(originalLocations: null);

            var view = viewContext.View = viewEngineResult.View;

            using (view as IDisposable)
            {
                await view.RenderAsync(viewContext);
            }

            return new HtmlString(viewContext.Writer.ToString());
        }

        private ViewEngineResult FindView(ActionContext actionContext, string viewName, bool isMainPage)
        {
            viewName ??= GetActionName(actionContext).EmptyNull();

            var result = _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: isMainPage);
            var getViewLocations = result.SearchedLocations;
            if (!result.Success)
            {
                result = _viewEngine.FindView(actionContext, viewName, isMainPage: isMainPage);
            }

            if (!result.Success)
            {
                var searchedLocations = Enumerable.Concat(getViewLocations, result.SearchedLocations);
                result = ViewEngineResult.NotFound(viewName, searchedLocations);
            }

            return result;
        }

        private static string? GetActionName(ActionContext context)
        {
            if (!context.RouteData.Values.TryGetValue(ActionNameKey, out var routeValue))
            {
                return null;
            }

            var actionDescriptor = context.ActionDescriptor;
            string? normalizedValue = null;
            if (actionDescriptor.RouteValues.TryGetValue(ActionNameKey, out var value) && !string.IsNullOrEmpty(value))
            {
                normalizedValue = value;
            }

            var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
            if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedValue;
            }

            return stringRouteValue;
        }
    }
}
