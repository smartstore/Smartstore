using System;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Bootstrapping;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class TaskSchedulerStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.AfterAuthenticationMiddleware, endpoints =>
            {
                endpoints.MapTaskScheduler();
            });
        }

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddTaskScheduler();
        }
    }
}
