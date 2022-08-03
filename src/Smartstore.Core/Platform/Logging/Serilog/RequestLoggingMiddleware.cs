using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog.Context;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Parsing;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging.Serilog;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using ILogger = Serilog.ILogger;
using SLog = Serilog.Log;

// INFO: *.Serilog omitted on purpose.
namespace Smartstore.Core.Logging
{
    internal class RequestLoggingMiddleware
    {
        readonly RequestDelegate _next;
        readonly DiagnosticContext _diagnosticContext;
        readonly MessageTemplate _messageTemplate;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            DiagnosticContext diagnosticContext)
        {
            _next = Guard.NotNull(next, nameof(next));
            _diagnosticContext = Guard.NotNull(diagnosticContext, nameof(diagnosticContext));

            _messageTemplate = new MessageTemplateParser()
                .Parse("HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms");
        }

        public async Task InvokeAsync(HttpContext httpContext, IWebHelper webHelper, IWorkContext workContext)
        {
            Guard.NotNull(httpContext, nameof(httpContext));

            var customerIdEnricher = new DelegatingPropertyEnricher("CustomerId",
                () => httpContext.Response.HasStarted ? null : workContext.CurrentCustomer?.Id);

            var userNameEnricher = new DelegatingPropertyEnricher("UserName",
                () => httpContext.User?.Identity?.Name);

            using (LogContext.PushProperty("Url", webHelper.GetCurrentPageUrl(true)))
            using (LogContext.PushProperty("Referrer", webHelper.GetUrlReferrer()?.OriginalString))
            using (LogContext.PushProperty("HttpMethod", httpContext.Request.Method))
            using (LogContext.PushProperty("Ip", webHelper.GetClientIpAddress().ToString()))
            using (LogContext.Push(customerIdEnricher))
            using (LogContext.Push(userNameEnricher))
            {
                var start = Stopwatch.GetTimestamp();

                var collector = _diagnosticContext.BeginCollection();
                try
                {
                    await _next.Invoke(httpContext);

                    var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                    var statusCode = httpContext.Response.StatusCode;
                    LogCompletion(httpContext, collector, statusCode, elapsedMs, null);
                }
                catch (Exception ex)
                    // Never caught, because `LogCompletion()` returns false.
                    // This ensures e.g. the developer exception page is still shown.
                    when (LogCompletion(httpContext, collector, 500, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex))
                {
                }
                finally
                {
                    // Fetch and freeze values as we'll go out of scope soon.
                    customerIdEnricher.FreezeValue();
                    userNameEnricher.FreezeValue();

                    collector.Dispose();
                }
            }
        }

        bool LogCompletion(HttpContext httpContext, DiagnosticContextCollector collector, int statusCode, double elapsedMs, Exception ex)
        {
            var features = httpContext.Features;
            if (features.Get<IExceptionHandlerPathFeature>() != null || features.Get<IStatusCodeReExecuteFeature>() != null)
            {
                // Don't execute again during re-execution.
                return false;
            }

            var endpoint = httpContext.GetEndpoint();
            var logger = GetLogger(endpoint, ex);
            var level = GetLevel(httpContext, ex);

            if (!logger.IsEnabled(level))
            {
                return false;
            }

            if (ex != null)
            {
                logger.Write(level, ex, GetMessage(httpContext, ex));
            }
            else
            {
                if (!collector.TryComplete(out var collectedProperties, out _))
                {
                    collectedProperties = Array.Empty<LogEventProperty>();
                }

                // Last-in (correctly) wins...
                var properties = collectedProperties.Concat(new[]
                {
                    new LogEventProperty("RequestPath", new ScalarValue(httpContext.Request.Path.Value)),
                    new LogEventProperty("StatusCode", new ScalarValue(statusCode)),
                    new LogEventProperty("Elapsed", new ScalarValue(elapsedMs)),
                    new LogEventProperty("HttpMethod", new ScalarValue(httpContext.Request.Method))
                });

                var evt = new LogEvent(DateTimeOffset.Now, level, ex, _messageTemplate, properties);
                logger.Write(evt);
            }

            return false;
        }

        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }

        static ILogger GetLogger(Endpoint endpoint, Exception ex)
        {
            var loggerType = typeof(RequestLoggingMiddleware);

            if (ex != null)
            {
                var exceptionSource = ex.TargetSite?.DeclaringType;

                if (exceptionSource != null)
                {
                    loggerType = exceptionSource;
                }
                else if (endpoint != null)
                {
                    var actionDescriptor = endpoint.Metadata.OfType<ControllerActionDescriptor>().FirstOrDefault();
                    if (actionDescriptor != null)
                    {
                        loggerType = actionDescriptor.ControllerTypeInfo.AsType();
                    }
                }
            }

            return SLog.ForContext(loggerType);
        }

        static LogEventLevel GetLevel(HttpContext ctx, Exception ex)
        {
            return ex != null
                ? (ex is AccessDeniedException ? LogEventLevel.Information : LogEventLevel.Error)
                : ctx.Response.StatusCode > 499
                    ? LogEventLevel.Error
                    : LogEventLevel.Debug;
        }

        static string GetMessage(HttpContext ctx, Exception ex)
        {
            if (ex is AccessDeniedException)
            {
                var identity = ctx.Features.Get<IHttpAuthenticationFeature>()?.User?.Identity;
                if (identity != null)
                {
                    var T = ctx.RequestServices.GetService<Localizer>();
                    if (T != null)
                    {
                        var path = ctx.Request.Path.Value;
                        return identity.IsAuthenticated
                            ? T("Admin.System.Warnings.AccessDeniedToUser", identity.Name.NaIfEmpty(), identity.Name.NaIfEmpty(), path.NaIfEmpty())
                            : T("Admin.System.Warnings.AccessDeniedToAnonymousRequest", path.NaIfEmpty());
                    }
                }
            }

            return ex.Message ?? "An unhandled exception has occurred while executing the request.";
        }
    }
}
