using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Smartstore.Engine;
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
        public static ContainerBuilder AddTaskScheduler<TStore>(this ContainerBuilder container, IApplicationContext appContext)
            where TStore : class, ITaskStore
        {
            Guard.NotNull(container, nameof(container));

            container.RegisterModule(new SchedulerModule(appContext));
            container.RegisterType<TStore>().As<ITaskStore>().InstancePerLifetimeScope();

            return container;
        }
    }
}
