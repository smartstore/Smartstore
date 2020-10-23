using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Smartstore.Web;

namespace Smartstore.Core.Logging.Serilog
{
    public class SerilogHttpContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogHttpContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IWebHelper webHelper)
        {
            // TODO: (core) Put request data to a single object and push it to LogContext

            using (LogContext.PushProperty("CustomerId", 1)) // TODO: (core) Put CustomerId to LogContext
            using (LogContext.PushProperty("UserName", (string)null)) // TODO: (core) Put UserName to LogContext
            using (LogContext.PushProperty("Url", webHelper.GetCurrentPageUrl(true)))
            using (LogContext.PushProperty("Referrer", webHelper.GetUrlReferrer()))
            using (LogContext.PushProperty("HttpMethod", context?.Request.Method))
            using (LogContext.PushProperty("Ip", webHelper.GetClientIpAddress().ToString()))
            {
                await _next.Invoke(context);
            }
        }
    }
}
