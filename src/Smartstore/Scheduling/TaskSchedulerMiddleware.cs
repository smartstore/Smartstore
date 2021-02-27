using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Scheduling
{
    public class TaskSchedulerMiddleware
    {
        private readonly RequestDelegate _next;

        public TaskSchedulerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // TODO: (core) Implement TaskSchedulerMiddleware
            await _next(context);
        }
    }
}
