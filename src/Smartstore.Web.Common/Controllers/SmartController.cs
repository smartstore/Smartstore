using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Web.Razor;

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

        public ICommonServices Services { get; set; }

        public IViewInvoker ViewInvoker
        {
            get => HttpContext.RequestServices.GetService<IViewInvoker>();
        }

        #region Widget, View & Component rendering

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        protected async Task<string> InvokeViewAsync(string viewName, object model)
            => (await ViewInvoker.InvokeViewAsync(viewName, null, new ViewDataDictionary<object>(ViewData, model))).ToString();

        ///// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        ///// <param name="model">Model</param>
        //public static Task<HtmlString> InvokeViewAsync(this IViewInvoker invoker, string viewName, string module, object model)
        //    => invoker.InvokeViewAsync(viewName, module, new ViewDataDictionary<object>(ViewData, model));

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        protected async Task<string> InvokeViewAsync(string viewName, ViewDataDictionary viewData)
            => (await ViewInvoker.InvokeViewAsync(viewName, null, viewData)).ToString();

        /// <inheritdoc cref="IViewInvoker.InvokeViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        /// <param name="additionalViewData">Additional view data</param>
        protected async Task<string> InvokeViewAsync(string viewName, object model, object additionalViewData)
            => (await ViewInvoker.InvokeViewAsync(viewName, model, additionalViewData)).ToString();


        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        protected async Task<string> InvokePartialViewAsync(string viewName, object model)
            => (await ViewInvoker.InvokePartialViewAsync(viewName, null, new ViewDataDictionary<object>(ViewData, model))).ToString();

        ///// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        ///// <param name="model">Model</param>
        //public static Task<HtmlString> InvokePartialViewAsync(this IViewInvoker invoker, string viewName, string module, object model)
        //    => invoker.InvokePartialViewAsync(viewName, module, new ViewDataDictionary<object>(ViewData, model));

        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        protected async Task<string> InvokePartialViewAsync(string viewName, ViewDataDictionary viewData)
            => (await ViewInvoker.InvokePartialViewAsync(viewName, null, viewData)).ToString();

        /// <inheritdoc cref="IViewInvoker.InvokePartialViewAsync(string, string, ViewDataDictionary)"/>
        /// <param name="model">Model</param>
        /// <param name="additionalViewData">Additional view data</param>
        protected async Task<string> InvokePartialViewAsync(string viewName, object model, object additionalViewData)
            => (await ViewInvoker.InvokePartialViewAsync(viewName, model, additionalViewData)).ToString();


        /// <inheritdoc cref="IViewInvoker.InvokeComponentAsync(string, string, ViewDataDictionary, object)"/>
        /// <param name="model">Model</param>
        protected async Task<string> InvokeComponentAsync(string componentName, ViewDataDictionary viewData, object arguments)
            => (await ViewInvoker.InvokeComponentAsync(componentName, null, viewData, arguments)).ToString();

        /// <inheritdoc cref="IViewInvoker.InvokeComponentAsync(Type, string, ViewDataDictionary, object)"/>
        /// <param name="model">Model</param>
        protected async Task<string> InvokeComponentAsync(Type componentType, ViewDataDictionary viewData, object arguments)
            => (await ViewInvoker.InvokeComponentAsync(componentType, viewData, arguments)).ToString();

        /// <summary>
        /// Invokes a widget and returns its html content.
        /// </summary>
        /// <param name="widget">Widget to invoke.</param>
        /// <returns>Widget rendering result</returns>
        protected async Task<string> InvokeWidgetAsync(WidgetInvoker widget)
        {
            Guard.NotNull(widget, nameof(widget));

            var viewContext = new ViewContext(
                ControllerContext,
                NullView.Instance,
                ViewData,
                TempData,
                TextWriter.Null,
                HttpContext.RequestServices.GetRequiredService<IOptions<MvcViewOptions>>().Value.HtmlHelperOptions
            );

            var result = await widget.InvokeAsync(viewContext);
            return result.ToHtmlString().ToString();
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

        protected ActionResult RedirectToReferrer(string referrer)
        {
            return RedirectToReferrer(referrer, () => RedirectToRoute("Homepage"));
        }

        protected ActionResult RedirectToReferrer(string referrer, string fallbackUrl)
        {
            return RedirectToReferrer(
                referrer,
                fallbackUrl.HasValue() ? () => Redirect(fallbackUrl) : null);
        }

        protected virtual ActionResult RedirectToReferrer(string referrer, Func<ActionResult> fallbackResult)
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
