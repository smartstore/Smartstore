using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Smartstore.Core.Widgets
{
    public class PartialViewWidgetInvoker : WidgetInvoker
    {
        private readonly string _partialName;
        private readonly string _module;

        /// <summary>
        /// Creates a new instance of <see cref="PartialViewWidgetInvoker"/>.
        /// </summary>
        /// <param name="partialName">Name of partial view to invoke.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        public PartialViewWidgetInvoker(string partialName, string module = null)
        {
            Guard.NotEmpty(partialName, nameof(partialName));

            _partialName = partialName;
            _module = module;
        }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
            return await viewInvoker.InvokePartialViewAsync(_partialName, module: _module, viewData: viewContext.ViewData);
        }

        public override async Task<IHtmlContent> InvokeAsync(ViewContext viewContext, object model)
        {
            var viewInvoker = viewContext.HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
            return await viewInvoker.InvokePartialViewAsync(_partialName, module: _module, model: model);
        }
    }
}
