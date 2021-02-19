using Autofac;
using Smartstore.Core.Content.Menus;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public class MenuStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterType<MenuStorage>().As<IMenuStorage>().InstancePerLifetimeScope();
            builder.RegisterType<LinkResolver>().As<ILinkResolver>().InstancePerLifetimeScope();
            builder.RegisterType<MenuPublisher>().As<IMenuPublisher>().InstancePerLifetimeScope();
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
                var registration = builder.RegisterType(type)
                    .As<IMenuItemProvider>()
                    .PropertiesAutowired(PropertyWiringOptions.None)
                    .InstancePerLifetimeScope()
                    .WithMetadata<MenuItemProviderMetadata>(m =>
                    {
                        m.For(em => em.ProviderName, attribute?.ProviderName);
                        m.For(em => em.AppendsMultipleItems, attribute?.AppendsMultipleItems ?? false);
                    });
            }
        }
    }
}