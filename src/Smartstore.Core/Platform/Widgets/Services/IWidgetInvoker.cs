#nullable enable

using Autofac.Core;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Smartstore.Core.Web;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Defines an interface for a service which can invoke a particular kind of <see cref="Widget"/>.
    /// </summary>
    /// <remarks>
    /// Implementations of <see cref="IWidgetInvoker{TWidget}"/> are typically called by the
    /// <see cref="Widget.InvokeAsync(WidgetContext)"/> method of the corresponding widget type.
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

    /// <summary>
    /// Abstract base class widget invoker services.
    /// </summary>
    public abstract class WidgetInvoker<TWidget> : IWidgetInvoker<TWidget> where TWidget : notnull, Widget
    {
        /// <inheritdoc/>
        public abstract Task<IHtmlContent> InvokeAsync(WidgetContext context, TWidget widget);

        /// <summary>
        /// Creates a defensive <see cref="ViewContext"/> copy so that changes made here
        /// aren't visible to the calling view.
        /// </summary>
        protected virtual ViewContext CreateViewContext(WidgetContext context, TextWriter writer, object? model, string? module)
        {
            var services = context.HttpContext.RequestServices;
            var actionContext = context.ActionContext;

            if (module.HasValue())
            {
                actionContext = new ActionContext(actionContext);
                actionContext.RouteData = new RouteData(actionContext.RouteData);
                actionContext.RouteData.DataTokens["module"] = module;
            }

            var tempData = context.TempData ?? services.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(context.HttpContext);

            var viewData = model != null || context.Model != null
                ? new ViewDataDictionary<object>(GetOrCreateViewData(), model ?? context.Model)
                : GetOrCreateViewData();

            var viewContext = new ViewContext(
                actionContext,
                NullView.Instance,
                viewData,
                tempData,
                writer,
                services.GetRequiredService<IOptions<MvcViewOptions>>().Value.HtmlHelperOptions);

            return viewContext;

            ViewDataDictionary GetOrCreateViewData()
            {
                // If WidgetContext.ActionContext is NOT ViewContext, we won't probably have ViewData
                return context.ViewData ??
                    services.GetRequiredService<IViewDataAccessor>().ViewData ??
                    new ViewDataDictionary(services.GetRequiredService<IModelMetadataProvider>(), actionContext.ModelState);
            }
        }
    }
}
