using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Scheduling;

namespace Smartstore.Bootstrapping
{
    public static class SchedulerBuilderExtensions
    {
        /// <summary>
        /// Maps task scheduler endpoints
        /// </summary>
        public static IEndpointConventionBuilder MapTaskScheduler(this IEndpointRouteBuilder endpoints)
        {
            Guard.NotNull(endpoints, nameof(endpoints));

            var schedulerMiddleware = endpoints.CreateApplicationBuilder()
               .UseMiddleware<TaskSchedulerMiddleware>()
               .Build();

            // Match scheduler URL pattern: e.g. '/taskscheduler/poll/', '/taskscheduler/execute/99', '/taskscheduler/noop' (GET)
            return endpoints.Map("taskscheduler/{action:regex(^poll|execute|noop$)}/{id:int?}", schedulerMiddleware)
                .WithMetadata(new HttpMethodMetadata(new[] { "GET", "POST" }))
                .WithDisplayName("Task scheduler");
        }
    }
}
