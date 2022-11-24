#nullable enable

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.OData.UriParser;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Utilities;

namespace Smartstore.Web.Controllers
{
    [MenuFilter]
    [NotifyFilter(Order = 1000)] // Run last (OnResultExecuting)
    public abstract class SmartController : Controller
    {
        protected SmartController()
        {
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public ICommonServices Services { get; set; } = default!;

        //public IViewInvoker ViewInvoker
        //{
        //    get => HttpContext.RequestServices.GetRequiredService<IViewInvoker>();
        //}

        #region Widget, View & Component rendering

        /// <summary>
        /// Invokes a view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="model">Model to pass to view.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokeViewAsync(string viewName, object? model)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(null, new PartialViewWidget(viewName, model) { IsMainPage = true });
        }

        /// <summary>
        /// Invokes a view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokeViewAsync(string viewName, ViewDataDictionary? viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(viewData, new PartialViewWidget(viewName) { IsMainPage = true });
        }

        /// <summary>
        /// Invokes a partial view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="model">Model to pass to view.</param>
        /// <param name="additionalViewData">Additional view data.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokeViewAsync(string viewName, object? model, object? additionalViewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(GetViewData(additionalViewData), new PartialViewWidget(viewName, model) { IsMainPage = true });
        }


        /// <summary>
        /// Invokes a partial view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="model">Model to pass to view.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokePartialViewAsync(string viewName, object? model)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(null, new PartialViewWidget(viewName, model));
        }

        /// <summary>
        /// Invokes a partial view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokePartialViewAsync(string viewName, ViewDataDictionary? viewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(viewData, new PartialViewWidget(viewName));
        }

        /// <summary>
        /// Invokes a partial view and returns its HTML result content as string.
        /// </summary>
        /// <param name="viewName">The name of view to invoke.</param>
        /// <param name="model">Model to pass to view.</param>
        /// <param name="additionalViewData">Additional view data.</param>
        /// <returns>View rendering result</returns>
        protected Task<string> InvokePartialViewAsync(string viewName, object? model, object? additionalViewData)
        {
            Guard.NotEmpty(viewName, nameof(viewName));

            return InvokeWidget(GetViewData(additionalViewData), new PartialViewWidget(viewName, model));
        }


        /// <summary>
        /// Invokes a view component and returns its HTML result content as string.
        /// </summary>
        /// <param name="componentName">The name of component to invoke.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        protected Task<string> InvokeComponentAsync(string componentName, ViewDataDictionary? viewData, object? arguments)
        {
            Guard.NotEmpty(componentName, nameof(componentName));
            
            return InvokeWidget(viewData, new ComponentWidget(componentName, arguments));
        }

        /// <summary>
        /// Invokes a view component and returns its HTML result content as string.
        /// </summary>
        /// <param name="componentType">The type of component to invoke.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/> instance.</param>
        /// <param name="arguments">
        /// An <see cref="object"/> with properties representing arguments to be passed to the invoked view component
        /// method. Alternatively, an <see cref="IDictionary{String, Object}"/> instance
        /// containing the invocation arguments.
        /// </param>
        /// <returns>View component rendering result</returns>
        protected Task<string> InvokeComponentAsync(Type componentType, ViewDataDictionary? viewData, object? arguments)
        {
            Guard.NotNull(componentType, nameof(componentType));

            return InvokeWidget(viewData, new ComponentWidget(componentType, arguments));
        }


        private async Task<string> InvokeWidget(ViewDataDictionary? viewData, Widget widget)
        {
            var context = new WidgetContext(ControllerContext)
            {
                ViewData = viewData ?? ViewData,
                TempData = TempData
            };

            return (await widget.InvokeAsync(context)).ToHtmlString().ToString();
        }

        private ViewDataDictionary GetViewData(object? additionalViewData)
        {
            var viewData = ViewData;
            if (additionalViewData != null)
            {
                viewData = new ViewDataDictionary<object>(viewData);
                viewData!.Merge(ConvertUtility.ObjectToDictionary(additionalViewData));

                if (additionalViewData is ViewDataDictionary vdd)
                {
                    viewData.TemplateInfo.HtmlFieldPrefix = vdd.TemplateInfo.HtmlFieldPrefix;
                }
            }

            return viewData;
        }

        #endregion

        #region Notify

        /// <summary>
        /// Pushes an info message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyInfo(string message, bool durable = true)
        {
            Services.Notifier.Information(message, durable);
        }

        /// <summary>
        /// Pushes a warning message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyWarning(string message, bool durable = true)
        {
            Services.Notifier.Warning(message, durable);
        }

        /// <summary>
        /// Pushes a success message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifySuccess(string message, bool durable = true)
        {
            Services.Notifier.Success(message, durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyError(string message, bool durable = true)
        {
            Services.Notifier.Error(message, durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="durable">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether the exception should be logged</param>
        protected virtual void NotifyError(Exception exception, bool durable = true, bool logException = true)
        {
            if (logException)
            {
                LogException(exception);
            }

            Services.Notifier.Error(exception.ToAllMessages().HtmlEncode(), durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue that the access to a resource has been denied
        /// </summary>
        /// <param name="durable">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="log">A value indicating whether the message should be logged</param>
        protected virtual void NotifyAccessDenied(bool durable = true, bool log = true)
        {
            var message = T("Admin.AccessDenied.Description");

            if (log)
            {
                Logger.Error(message);
            }

            Services.Notifier.Error(message, durable);
        }

        #endregion

        #region Redirection

        protected ActionResult RedirectToReferrer()
        {
            return RedirectToReferrer(null, () => RedirectToRoute("Homepage"));
        }

        protected ActionResult RedirectToReferrer(string? referrer)
        {
            return RedirectToReferrer(referrer, () => RedirectToRoute("Homepage"));
        }

        protected ActionResult RedirectToReferrer(string? referrer, string? fallbackUrl)
        {
            return RedirectToReferrer(
                referrer,
                fallbackUrl.HasValue() ? () => Redirect(fallbackUrl!) : null);
        }

        protected virtual ActionResult RedirectToReferrer(string? referrer, Func<ActionResult>? fallbackResult)
        {
            referrer ??= Url.Referrer();

            if (referrer.HasValue() && !referrer.EqualsNoCase(Request.RawUrl()))
            {
                return Redirect(referrer);
            }

            if (fallbackResult != null)
            {
                return fallbackResult();
            }

            return NotFound();
        }

        #endregion

        #region Exceptions

        /// <summary>
        /// Logs an exception
        /// </summary>
        private void LogException(Exception ex)
        {
            Logger.Error(ex);
        }

        #endregion
    }
}
