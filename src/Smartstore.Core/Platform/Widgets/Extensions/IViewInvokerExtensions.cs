#nullable enable

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Widgets;

namespace Smartstore
{
    public static class IViewInvokerExtensions
    {
        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name or path. Will be expanded if it starts with <c>[~]/{theme}/</c>.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, string? module, ViewDataDictionary? viewData)
        {
            Guard.NotEmpty(viewName);

            var actionContext = invoker.GetActionContext(null, module);
            var result = new ViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? invoker.ViewData!
            };

            return invoker.InvokeViewAsync(actionContext, result);
        }

        /// <summary>
        /// Invokes a partial view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name or path. Will be expanded if it starts with <c>[~]/{theme}/</c>.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, string? module, ViewDataDictionary? viewData)
        {
            Guard.NotEmpty(viewName);

            var actionContext = invoker.GetActionContext(null, module);
            var result = new PartialViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? invoker.ViewData!
            };

            return invoker.InvokePartialViewAsync(actionContext, result);
        }
    }
}
