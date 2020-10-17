using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Smartstore.Core.Logging
{
    public class SerilogLocalContextMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogLocalContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // TODO: (core) Put request data to a single object and push it to LogContext

            using (LogContext.PushProperty("CustomerId", 1))
            {
                await _next.Invoke(context);
            }
        }
    }
}
