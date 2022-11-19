using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidget : Widget
    {
        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidget"/>.
        /// </summary>
        /// <param name="partialName">Name of partial view to invoke.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidget(string partialName, string module = null)
        {
            Guard.NotEmpty(partialName, nameof(partialName));

            PartialName = partialName;
            Module = module;
        }

        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidget"/>.
        /// </summary>
        /// <param name="partialName">Name of partial view to invoke.</param>
        /// <param name="model">Model instance to pass to partial view..</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidget(string partialName, object model, string module = null)
        {
            Guard.NotEmpty(partialName, nameof(partialName));

            PartialName = partialName;
            Module = module;
            Model = model;
        }

        public string PartialName { get; }
        public string Module { get; }
        public object Model { get; }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var viewData = model == null
                ? viewContext.ViewData
                : new ViewDataDictionary<object>(viewContext.ViewData, model);

            var viewInvoker = viewContext.HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
            return await viewInvoker.InvokePartialViewAsync(
                PartialName,
                module: Module,
                viewData: viewData);
        }

        public override Task<IHtmlContent> Invoke2Async(WidgetContext context)
        {
            var invoker = context.HttpContext.RequestServices.GetRequiredService<IWidgetInvoker<PartialViewWidget>>();
            return invoker.InvokeAsync(context, this);
        }
    }
}
