using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore.Web.Components
{
    public abstract class SmartViewComponent : ViewComponent
    {
        public ILogger Logger
        {
            get => HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        }

        public Localizer T
        {
            get => HttpContext.RequestServices.GetRequiredService<IText>().Get;
        }

        public ICommonServices Services 
        {
            get => HttpContext.RequestServices.GetRequiredService<ICommonServices>();
        }

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
