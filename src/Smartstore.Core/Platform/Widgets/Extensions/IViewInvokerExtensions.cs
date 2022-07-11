using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Widgets;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class IViewInvokerExtensions
    {
        #region View

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

        /// <summary>
        /// Invokes a view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, string module, ViewDataDictionary viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            var actionContext = invoker.GetActionContext(null, module);
            var result = new ViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? invoker.ViewData
            };

            return ExecuteCapturedAsync(writer => invoker.InvokeViewAsync(actionContext, result, writer));
        }

        #endregion

        #region Partial view

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

        /// <summary>
        /// Invokes a partial view and returns its html content.
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance that also contains the model.</param>
        /// <returns>View rendering result</returns>
        public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, string module, ViewDataDictionary viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            var actionContext = invoker.GetActionContext(null, module);
            var result = new PartialViewResult
            {
                ViewName = viewName,
                ViewData = viewData ?? invoker.ViewData
            };

            return ExecuteCapturedAsync(writer => invoker.InvokePartialViewAsync(actionContext, result, writer));
        }

        #endregion

        #region Component

        /// <inheritdoc cref="IViewInvoker.InvokeComponentAsync(string, string, ViewDataDictionary, object)"/>
        public static Task<HtmlString> InvokeComponentAsync(this IViewInvoker invoker, string componentName, ViewDataDictionary viewData, object arguments)
        {
            return invoker.InvokeComponentAsync(componentName, null, viewData, arguments);
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="componentName">The view component name.</param>
        /// <param name="module">Optional: system name of a module to additionally search for view files in.</param>
        /// <param name="viewData">View name</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<HtmlString> InvokeComponentAsync(this IViewInvoker invoker, string componentName, string module, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));

            var actionContext = invoker.GetActionContext(null, module);
            viewData ??= invoker.ViewData;
            var result = new ViewComponentResult
            {
                ViewComponentName = componentName,
                Arguments = arguments,
                ViewData = viewData
            };

            return ExecuteCapturedAsync(writer => invoker.InvokeComponentAsync(actionContext, result, writer));
        }

        /// <summary>
        /// Invokes a view component and returns its html content.
        /// </summary>
        /// <param name="componentType">The view component type.</param>
        /// <param name="viewData">View name</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        public static Task<HtmlString> InvokeComponentAsync(this IViewInvoker invoker, Type componentType, ViewDataDictionary viewData, object arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));

            var moduleDescriptor = EngineContext.Current.Application.ModuleCatalog.GetModuleByAssembly(componentType.Assembly);
            var moduleName = moduleDescriptor?.SystemName;

            var actionContext = invoker.GetActionContext(null, moduleName);
            viewData ??= invoker.ViewData;
            var result = new ViewComponentResult
            {
                ViewComponentType = componentType,
                Arguments = arguments,
                ViewData = viewData
            };

            return ExecuteCapturedAsync(writer => invoker.InvokeComponentAsync(actionContext, result, writer));
        }

        #endregion

        private static async Task<HtmlString> ExecuteCapturedAsync(Func<TextWriter, Task> executor)
        {
            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using (var writer = new StringWriter(sb))
            {
                await executor(writer);
            }

            return new HtmlString(sb.ToString());
        }
    }
}
