using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;

namespace Smartstore.Web.Controllers
{
    // TODO: (core) Implement base filters for SmartController
    public abstract class SmartController : Controller
    {
        protected SmartController()
        {
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public ICommonServices Services { get; set; }

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
            // addressing "Open Redirection Vulnerability" (prevent cross-domain redirects / phishing)
            if (fallbackUrl.HasValue() && !Url.IsLocalUrl(fallbackUrl))
            {
                fallbackUrl = null;
            }

            return RedirectToReferrer(
                referrer,
                fallbackUrl.HasValue() ? () => Redirect(fallbackUrl) : null);
        }

        protected virtual ActionResult RedirectToReferrer(string referrer, Func<ActionResult> fallbackResult)
        {
            bool skipLocalCheck = false;
            var requestReferrer = Services.WebHelper.GetUrlReferrer();

            if (referrer.IsEmpty() && requestReferrer.HasValue())
            {
                referrer = requestReferrer;
                var domain1 = (new Uri(referrer)).GetLeftPart(UriPartial.Authority);
                var domain2 = Request.Scheme + Uri.SchemeDelimiter + Request.Host;
                if (domain1.EqualsNoCase(domain2))
                {
                    // always allow fully qualified urls from local host
                    skipLocalCheck = true;
                }
                else
                {
                    referrer = null;
                }
            }

            // addressing "Open Redirection Vulnerability" (prevent cross-domain redirects / phishing)
            if (referrer.HasValue() && !skipLocalCheck && !Url.IsLocalUrl(referrer))
            {
                referrer = null;
            }

            if (referrer.HasValue())
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

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null && !context.ExceptionHandled)
            {
                LogException(context.Exception);
            }
        }

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
