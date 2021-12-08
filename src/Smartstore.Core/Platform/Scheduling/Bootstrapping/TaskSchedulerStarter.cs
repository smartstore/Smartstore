using System;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Bootstrapping;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Scheduling;

namespace Smartstore.Core.Bootstrapping
{
    internal class TaskSchedulerStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.EarlyRoute, endpoints =>
            {
                endpoints.MapTaskScheduler();
            });
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext)
        {
            services.AddHttpClient(DefaultTaskScheduler.HttpClientName, client =>
            {
                // INFO: avoids HttpClient.Timeout error messages in the log list.
                // Only affects the HTTP request that starts the task. Does not affect the execution of the task.
                client.Timeout = TimeSpan.FromMinutes(240);
            });
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.AddTaskScheduler<DbTaskStore>(appContext);
            builder.RegisterType<TaskContextVirtualizer>().As<ITaskContextVirtualizer>().InstancePerLifetimeScope();
        }
    }
}
