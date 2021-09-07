using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Forum
{
    internal class Startup : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
            => appContext.IsInstalled;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            if (!isActiveModule)
            {
                return;
            }

            //services.AddDbContext<ForumDbContext>(builder => builder
            //    .UseSecondLevelCache()
            //    .UseDbFactory());
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(0, routes =>
            {
                routes.MapControllerRoute(
                    Module.SystemName,
                    "Module/Smartstore.Forum/{controller}/{action}/{id?}",
                    new { controller = "Forum", action = "Configure" });
            });
        }
    }
}
