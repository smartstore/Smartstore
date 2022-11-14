using Autofac;
using Microsoft.AspNetCore.Builder;
using Smartstore.Engine;
using Smartstore.Scheduling;

namespace Smartstore.Bootstrapping
{
    public static class SchedulerBootstrappingExtensions
    {
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

        /// <summary>
        /// Maps task scheduler middleware as a new branch.
        /// </summary>
        /// <param name="configure">
        /// Add custom middlewares BEFORE the task scheduler middleware.
        /// </param>
        public static IApplicationBuilder MapTaskScheduler(this IApplicationBuilder app, Action<IApplicationBuilder> configure = null)
        {
            Guard.NotNull(app, nameof(app));

            return app.Map("/taskscheduler", preserveMatchedPathSegment: true, branch =>
            {
                configure?.Invoke(branch);
                branch.UseMiddleware<TaskSchedulerMiddleware>();
            });
        }
    }
}
