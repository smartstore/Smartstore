using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidgetInvoker : WidgetInvoker
    {
        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidgetInvoker"/>.
        /// </summary>
        /// <param name="partialName">Name of partial view to invoke.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidgetInvoker(string partialName, string module = null)
        {
            Guard.NotEmpty(partialName, nameof(partialName));

            PartialName = partialName;
            Module = module;
        }

        public string PartialName { get; }
        public string Module { get; }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
            return await viewInvoker.InvokePartialViewAsync(PartialName, module: Module, viewData: viewContext.ViewData);
        }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
            return await viewInvoker.InvokePartialViewAsync(PartialName, module: Module, model: model);
        }
    }
}
