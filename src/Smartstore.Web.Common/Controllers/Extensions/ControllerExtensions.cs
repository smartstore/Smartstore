using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Utilities;
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
        public static Task<string> InvokeViewAsync(this ControllerBase controller, string viewName, bool isPartial = true)
        {
            return InvokeViewAsync(controller, viewName, (object)null, isPartial);
        }

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="model">Model</param>
        /// <param name="isPartial"><c>true</c>: View is partial, otherwise main page.</param>
        /// <returns>View rendering result</returns>
        public static Task<string> InvokeViewAsync(this ControllerBase controller, string viewName, object model, bool isPartial = true)
        {
            Guard.NotNull(controller, nameof(controller));

            viewName = viewName.NullEmpty() ?? controller.ControllerContext.ActionDescriptor.ActionName;

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewAsync(viewName, model, isPartial);
        }

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="model">Model</param>
        /// <param name="additionalViewData">Additional view data</param>
        /// <param name="isPartial"><c>true</c>: View is partial, otherwise main page.</param>
        /// <returns>View rendering result</returns>
        public static Task<string> InvokeViewAsync(this Controller controller, string viewName, object model, object additionalViewData, bool isPartial = true)
        {
            Guard.NotNull(controller, nameof(controller));

            controller.ViewData.Model = model;

            if (additionalViewData != null)
            {
                controller.ViewData.Merge(CommonHelper.ObjectToDictionary(additionalViewData));

                if (additionalViewData is ViewDataDictionary vdd)
                {
                    controller.ViewData.TemplateInfo.HtmlFieldPrefix = vdd.TemplateInfo.HtmlFieldPrefix;
                }
            }

            return InvokeViewAsync(controller, viewName, controller.ViewData, isPartial);
        }

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <param name="isPartial"><c>true</c>: View is partial, otherwise main page.</param>
        /// <returns>View rendering result</returns>
        public static Task<string> InvokeViewAsync(this ControllerBase controller, string viewName, ViewDataDictionary viewData, bool isPartial = true)
        {
            Guard.NotNull(controller, nameof(controller));

            viewName = viewName.NullEmpty() ?? controller.ControllerContext.ActionDescriptor.ActionName;

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewAsync(viewName, viewData, isPartial);
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="componentName">The view component name.</param>
        /// <param name="viewData">View name</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<string> InvokeViewComponentAsync(this ControllerBase controller, string componentName, ViewDataDictionary viewData, object arguments = null)
        {
            Guard.NotNull(controller, nameof(controller));

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewComponentAsync(componentName, viewData, arguments);
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="componentType">The view component type.</param>
        /// <param name="viewData">View name</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<string> InvokeViewComponentAsync(this ControllerBase controller, Type componentType, ViewDataDictionary viewData, object arguments = null)
        {
            Guard.NotNull(controller, nameof(controller));

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeViewComponentAsync(componentType, viewData, arguments);
        }

        /// <summary>
        /// Invokes a widget and returns its html content.
        /// </summary>
        /// <param name="widget">Widget to invoke.</param>
        /// <returns>Widget rendering result</returns>
        public static Task<string> InvokeWidgetAsync(this ControllerBase controller, WidgetInvoker widget)
        {
            Guard.NotNull(controller, nameof(controller));

            var renderer = controller.HttpContext.RequestServices.GetRequiredService<IRazorViewInvoker>();
            return renderer.InvokeWidgetAsync(widget);
        }
    }
}