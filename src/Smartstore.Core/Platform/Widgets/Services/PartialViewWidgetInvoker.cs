#nullable enable

using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Smartstore.Core.Theming;
using Smartstore.Utilities;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidgetInvoker : WidgetInvoker<PartialViewWidget>
    {
        private const string ActionNameKey = "action";
        private const string ViewExtension = ".cshtml";

        private readonly ICompositeViewEngine _viewEngine;

        public PartialViewWidgetInvoker(ICompositeViewEngine viewEngine)
        {
            _viewEngine = viewEngine;
        }

        public override async Task<IHtmlContent> InvokeAsync(WidgetContext context, PartialViewWidget widget)
        {
            Guard.NotNull(context);
            Guard.NotNull(widget);

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

            ViewEngineResult result = default!;
            IEnumerable<string> searchedLocations = default!;

            if (IsApplicationRelativePath(viewName) || IsRelativePath(viewName))
            {
                // Path is specific, like "~/Views/Shared/File.cshtml"
                var executingFilePath = (actionContext as ViewContext)?.ExecutingFilePath;
                foreach (var path in ExpandViewPath(viewName, actionContext))
                {
                    result = _viewEngine.GetView(executingFilePath, viewPath: path, isMainPage: isMainPage);
                    if (result.Success)
                    {
                        break;
                    }

                    searchedLocations ??= new List<string>();
                    ((List<string>)searchedLocations).AddRange(result.SearchedLocations);
                }
            }
            else
            {
                // Path is generic, like "File", must resolve location
                result = _viewEngine.FindView(actionContext, viewName, isMainPage: isMainPage);
                searchedLocations = result.SearchedLocations;
            }

            if (!result.Success)
            {
                result = ViewEngineResult.NotFound(viewName, searchedLocations ?? result.SearchedLocations);
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

        /// <summary>
        /// Replaces {theme} token with current theme path and expands viewPath like this 
        /// (if viewPath is e.g. '/{theme}/Views/Shared/File.cshtml'):
        /// 1. /Themes/Flex/Views/Shared/File.cshtml
        /// 2. /Views/Shared/File.cshtml
        /// </summary>
        private static IEnumerable<string> ExpandViewPath(string viewPath, ActionContext actionContext)
        {
            viewPath = viewPath.TrimStart('~');
            
            if (viewPath.StartsWithNoCase("/{theme}/"))
            {
                // Strip off /{theme}
                var subpath = viewPath[8..];

                var workingTheme = actionContext.HttpContext.RequestServices.GetRequiredService<IThemeContext>().WorkingThemeName;

                // --> {/Themes/Flex}/Views/Shared/File.cshtml
                yield return "/Themes/" + workingTheme + subpath;

                // --> /Views/Shared/File.cshtml
                yield return subpath;
            }
            else
            {
                yield return viewPath;
            }        
        }

        private static bool IsApplicationRelativePath(string name)
        {
            return name[0] == '~' || name[0] == '/';
        }

        private static bool IsRelativePath(string name)
        {
            // Though ./ViewName looks like a relative path, framework searches for that view using view locations.
            return name.EndsWith(ViewExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
