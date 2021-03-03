using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Scheduling;

namespace Smartstore.Bootstrapping
{
    public static class SchedulerBootstrappingExtensions
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
            return endpoints.Map("taskscheduler/{action:regex(^poll|run|noop$)}/{id:int?}", schedulerMiddleware)
                .WithMetadata(new HttpMethodMetadata(new[] { "GET", "POST" }))
                .WithDisplayName("Task scheduler");
        }

        /// <summary>
        /// Add an <see cref="ITaskScheduler"/> registration as a hosted service.
        /// </summary>
        public static IServiceCollection AddTaskScheduler<TStore>(this IServiceCollection services)
            where TStore : class, ITaskStore
        {
            // TODO: (core) Pass ITaskStore impl type to AddTaskScheduler(). OR make TaskSchedulerOptions.
            Guard.NotNull(services, nameof(services));

            services.AddTransient<ITaskStore, TStore>();
            services.AddSingleton<ITaskScheduler, DefaultTaskScheduler>();
            services.AddHostedService(services => (DefaultTaskScheduler)services.GetRequiredService<ITaskScheduler>());
            return services;
        }
    }
}
