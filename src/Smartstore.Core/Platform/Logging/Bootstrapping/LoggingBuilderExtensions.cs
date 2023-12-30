using Microsoft.AspNetCore.Builder;
using Smartstore.Core.Logging;

namespace Smartstore.Core.Bootstrapping
{
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        /// Adds middleware for streamlined request logging. Instead of writing HTTP request information
        /// like method, path, timing, status code and exception details
        /// in several events, this middleware collects information during the request (including from
        /// <see langword="IDiagnosticContext"/>), and writes a single event at request completion. Add this
        /// in <c>Startup.cs</c> before any handlers whose activities should be logged.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            Guard.NotNull(app);

            app.UseMiddleware<RequestLoggingMiddleware>();
            return app;
        }
    }
}
