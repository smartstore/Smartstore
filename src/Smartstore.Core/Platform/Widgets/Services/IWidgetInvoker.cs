#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Defines an interface for a service which can invoke a particular kind of <see cref="Widget"/>.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="IWidgetInvoker{TWidget}"/> are typically called by the
    /// <see cref="Widget.InvokeAsync(ViewContext, object)"/> method of the corresponding widget type.
    /// Implementations should be registered as singleton services.
    /// </remarks>
    public interface IWidgetInvoker<in TWidget> where TWidget : notnull, Widget
    {
        /// <summary>
        /// Invokes the widget asynchronously and returns its content.
        /// </summary>
        /// <param name="context">Widget context.</param>
        /// <param name="widget">The widget to invoke.</param>
        /// <returns>The result HTML content.</returns>
        Task<IHtmlContent> InvokeAsync(WidgetContext context, TWidget widget);
    }

    public abstract class ModuleAwareWidgetInvoker<TWidget> : IWidgetInvoker<TWidget> where TWidget : notnull, Widget
    {
        public virtual ViewContext CreateViewContext(WidgetContext context, TextWriter writer, string? module)
        {
            var services = context.HttpContext.RequestServices;
            var actionContext = context.ActionContext;

            if (module.HasValue())
            {
                actionContext = new ActionContext(actionContext);
                actionContext.RouteData = new RouteData(actionContext.RouteData);
                actionContext.RouteData.DataTokens["module"] = module;
            }

            var viewData = context.ViewData ?? new ViewDataDictionary(services.GetRequiredService<IModelMetadataProvider>(), actionContext.ModelState);
            var tempData = context.TempData ?? services.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context.HttpContext);

            var viewContext = new ViewContext(
                actionContext,
                NullView.Instance,
                viewData,
                tempData,
                new StringWriter(),
                services.GetRequiredService<IOptions<MvcViewOptions>>().Value.HtmlHelperOptions);

            return viewContext;
        }

        public abstract Task<IHtmlContent> InvokeAsync(WidgetContext context, TWidget widget);
    }
}
