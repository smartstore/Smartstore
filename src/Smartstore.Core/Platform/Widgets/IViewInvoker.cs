using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Utilities;

namespace Smartstore.Core.Widgets
{
    /// <summary>
    /// Invokes and renders (partial) views and components outside of controllers.
    /// </summary>
    public interface IViewInvoker
    {
        ViewDataDictionary ViewData { get; }

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        Task<HtmlString> InvokeViewAsync(string viewName, string module, ViewDataDictionary viewData);

        /// <summary>
        /// Invokes a partial view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        Task<HtmlString> InvokePartialViewAsync(string viewName, string module, ViewDataDictionary viewData);

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="componentName">The view component name.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">View name</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="System.Collections.Generic.IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        Task<HtmlString> InvokeComponentAsync(string componentName, string module, ViewDataDictionary viewData, object arguments);

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
        Task<HtmlString> InvokeComponentAsync(Type componentType, ViewDataDictionary viewData, object arguments);
    }

    public static class IViewInvokerExtensions
    {
        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, object model)
        {
            return invoker.InvokeViewAsync(viewName, null, new ViewDataDictionary<object>(invoker.ViewData, model));
        }

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, string module, object model)
        {
            return invoker.InvokeViewAsync(viewName, module, new ViewDataDictionary<object>(invoker.ViewData, model));
        }

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, ViewDataDictionary viewData)
        {
            return invoker.InvokeViewAsync(viewName, null, viewData);
        }

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        /// <param name="additionalViewData">Additional view data</param>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, object model, object additionalViewData)
        {
            var viewData = new ViewDataDictionary<object>(invoker.ViewData, model);

            if (additionalViewData != null)
            {
                viewData.Merge(CommonHelper.ObjectToDictionary(additionalViewData));

                if (additionalViewData is ViewDataDictionary vdd)
                {
                    viewData.TemplateInfo.HtmlFieldPrefix = vdd.TemplateInfo.HtmlFieldPrefix;
                }
            }

            return invoker.InvokeViewAsync(viewName, null, viewData);
        }


        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, object model)
        {
            return invoker.InvokePartialViewAsync(viewName, null, new ViewDataDictionary<object>(invoker.ViewData, model));
        }

        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, string module, object model)
        {
            return invoker.InvokePartialViewAsync(viewName, module, new ViewDataDictionary<object>(invoker.ViewData, model));
        }

        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, ViewDataDictionary viewData)
        {
            return invoker.InvokePartialViewAsync(viewName, null, viewData);
        }

        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        /// <param name="additionalViewData">Additional view data</param>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, object model, object additionalViewData)
        {
            var viewData = new ViewDataDictionary<object>(invoker.ViewData, model);

            if (additionalViewData != null)
            {
                viewData.Merge(CommonHelper.ObjectToDictionary(additionalViewData));

                if (additionalViewData is ViewDataDictionary vdd)
                {
                    viewData.TemplateInfo.HtmlFieldPrefix = vdd.TemplateInfo.HtmlFieldPrefix;
                }
            }

            return invoker.InvokePartialViewAsync(viewName, null, viewData);
        }


        /// <inheritdoc cref="IViewInvoker.InvokeComponentAsync(string, string, ViewDataDictionary, object)"/>
        /// <param name="model">Model</param>
        public static Task<HtmlString> InvokeComponentAsync(this IViewInvoker invoker, string componentName, ViewDataDictionary viewData, object arguments)
        {
            return invoker.InvokeComponentAsync(componentName, null, viewData, arguments);
        }
    }
}
