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
        internal const string RootPath = "taskscheduler";
        internal const string PollAction = "poll";
        internal const string RunAction = "run";
        internal const string NoopAction = "noop";

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
