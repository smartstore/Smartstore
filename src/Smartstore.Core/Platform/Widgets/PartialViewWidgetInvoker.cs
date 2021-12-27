using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidgetInvoker"/>.
        /// </summary>
        /// <param name="partialName">Name of partial view to invoke.</param>
        /// <param name="model">Model instance to pass to partial view..</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidgetInvoker(string partialName, object model, string module = null)
        {
            Guard.NotEmpty(partialName, nameof(partialName));

            PartialName = partialName;
            Module = module;
            Model = model;
        }

        public string PartialName { get; }
        public string Module { get; }
        public object Model { get; }

        public override Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
            => InvokeAsync(viewContext, null);

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
    }
}
