using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core;
using Smartstore.Core.Content.Seo.Routing;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Web.Rendering;
using Smartstore.Web.TagHelpers;

namespace Smartstore.Web
{
    public class WebStarter : StarterBase
    {
        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddScoped<IWorkContext, WebWorkContext>();
            services.AddScoped<IPageAssetBuilder, PageAssetBuilder>();
            services.AddSingleton<IIconExplorer, IconExplorer>();
            services.AddScoped<IMenuPublisher, MenuPublisher>();
            services.AddScoped<SlugRouteTransformer>();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<MenuService>().As<IMenuService>().InstancePerLifetimeScope();

            var menuResolverTypes = appContext.TypeScanner.FindTypes<IMenuResolver>(ignoreInactiveModules: true);
            foreach (var type in menuResolverTypes)
            {
                builder.RegisterType(type).As<IMenuResolver>().PropertiesAutowired(PropertyWiringOptions.None).InstancePerLifetimeScope();
            }

            builder.RegisterType<DatabaseMenu>().Named<IMenu>("database").InstancePerDependency();

            var menuTypes = appContext.TypeScanner.FindTypes<IMenu>(ignoreInactiveModules: true);
            foreach (var type in menuTypes)
            {
                builder.RegisterType(type).As<IMenu>().PropertiesAutowired(PropertyWiringOptions.None).InstancePerLifetimeScope();
            }

            var menuItemProviderTypes = appContext.TypeScanner.FindTypes<IMenuItemProvider>(ignoreInactiveModules: true);
            foreach (var type in menuItemProviderTypes)
            {
                var attribute = type.GetAttribute<MenuItemProviderAttribute>(false);
                var registration = builder.RegisterType(type).As<IMenuItemProvider>().PropertiesAutowired(PropertyWiringOptions.None).InstancePerLifetimeScope();
                registration.WithMetadata<MenuItemProviderMetadata>(m =>
                {
                    m.For(em => em.ProviderName, attribute.ProviderName);
                    m.For(em => em.AppendsMultipleItems, attribute.AppendsMultipleItems);
                });
            }

            // TODO: (mh) (core) Uncomment when MenuActionFilter & MenuResultFilter are available.
            //if (DataSettings.DatabaseIsInstalled())
            //{
            //    // We have to register two classes, otherwise the filters would be called twice.
            //    builder.RegisterType<MenuActionFilter>().AsActionFilterFor<SmartController>(0);
            //    builder.RegisterType<MenuResultFilter>().AsResultFilterFor<SmartController>(0);
            //}
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

                // INFO: Test route
                // TODO: (mh) (core) Remove test route when not needed anymore.
                routes.MapLocalizedControllerRoute("RecentlyAddedProducts", "newproducts/", new { controller = "Catalog", action = "RecentlyAddedProducts" });

                // TODO: (core) Very last route: PageNotFound?
                routes.MapControllerRoute("PageNotFound", "{*path}", new { controller = "Error", action = "NotFound" });
            });
        }
    }
}