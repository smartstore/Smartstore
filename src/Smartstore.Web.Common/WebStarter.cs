using Autofac;
using Microsoft.AspNetCore.Builder;
using Smartstore.Core;
using Smartstore.Core.Content.Seo.Routing;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Web
{
    public class WebStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<WebWorkContext>().As<IWorkContext>().InstancePerLifetimeScope();
            builder.RegisterType<SlugRouteTransformer>().InstancePerLifetimeScope();
        }

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            if (!builder.ApplicationContext.IsInstalled)
            {
                return;
            }

            builder.MapRoutes(StarterOrdering.EarlyRoute, routes => 
            {
                routes.MapDynamicControllerRoute<SlugRouteTransformer>("{**slug:minlength(2)}");
            });
        }
    }

    public class LastRoutes : StarterBase
    {
        public override bool Matches(IApplicationContext appContext) 
            => appContext.IsInstalled;

        public override void MapRoutes(EndpointRoutingBuilder builder)
        {
            builder.MapRoutes(StarterOrdering.LastRoute, routes =>
            {
                // Register routes from SlugRouteTransformer solely needed for URL creation, NOT for route matching.
                SlugRouteTransformer.Routers.Each(x => x.MapRoutes(routes));

                // TODO: (core) Very last route: PageNotFound?
                routes.MapControllerRoute("PageNotFound", "{*path}", new { controller = "Error", action = "NotFound" });
            });
        }
    }
}