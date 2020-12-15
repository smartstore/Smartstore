using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Smartstore.Core.Web;

namespace Smartstore.Core.Logging.Serilog
{
    public class SerilogHttpContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogHttpContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IWebHelper webHelper, IWorkContext workContext)
        {
            using (LogContext.PushProperty("CustomerId", workContext.CurrentCustomer?.Id))
            using (LogContext.PushProperty("UserName", context.User.Identity.Name))
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
