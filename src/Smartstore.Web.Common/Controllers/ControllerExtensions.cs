using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Web.Razor;

namespace Smartstore.Web.Controllers
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="isMainPage"><c>false</c>: View is partial</param>
        /// <returns>View rendering result</returns>
        public static Task<IHtmlContent> InvokeViewAsync(this ControllerBase controller, string viewName, bool isMainPage = false)
        {
            return InvokeViewAsync<dynamic>(controller, viewName, null, isMainPage);
        }

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="model">Model</param>
        /// <param name="isMainPage"><c>false</c>: View is partial</param>
        /// <returns>View rendering result</returns>
        public static Task<IHtmlContent> InvokeViewAsync<TModel>(this ControllerBase controller, string viewName, TModel model, bool isMainPage = false)
        {
            Guard.NotNull(controller, nameof(controller));

            viewName = viewName.NullEmpty() ?? controller.ControllerContext.ActionDescriptor.ActionName;

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewAsync(viewName, model, isMainPage);
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="viewData">View name</param>
        /// <param name="componentName">The view component name.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<IHtmlContent> InvokeViewComponentAsync(this ControllerBase controller, ViewDataDictionary viewData, string componentName, object arguments)
        {
            Guard.NotNull(controller, nameof(controller));

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewComponentAsync(viewData, componentName, arguments);
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="viewData">View name</param>
        /// <param name="componentType">The view component type.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<IHtmlContent> InvokeViewComponentAsync(this ControllerBase controller, ViewDataDictionary viewData, Type componentType, object arguments)
        {
            Guard.NotNull(controller, nameof(controller));

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewComponentAsync(viewData, componentType, arguments);
        }
    }
}
