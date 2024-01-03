using Autofac;
using Smartstore.Core.Content.Menus;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal class MenuStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterType<MenuStorage>().As<IMenuStorage>().InstancePerLifetimeScope();
            builder.RegisterType<LinkResolver>().As<ILinkResolver>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultLinkProvider>().As<ILinkProvider>().InstancePerLifetimeScope();
            builder.RegisterType<MenuPublisher>().As<IMenuPublisher>().InstancePerLifetimeScope();
            builder.RegisterType<MenuService>().As<IMenuService>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultBreadcrumb>().As<IBreadcrumb>().InstancePerLifetimeScope();

            var menuResolverTypes = appContext.TypeScanner.FindTypes<IMenuResolver>();
            foreach (var type in menuResolverTypes)
            {
                builder.RegisterType(type).As<IMenuResolver>().InstancePerLifetimeScope();
            }

            builder.RegisterType<DatabaseMenu>().Named<IMenu>("database").InstancePerDependency();

            var menuTypes = appContext.TypeScanner.FindTypes<IMenu>();
            foreach (var type in menuTypes.Where(x => x.IsVisible && x != typeof(DatabaseMenu)))
            {
                builder.RegisterType(type).As<IMenu>().InstancePerLifetimeScope();
            }

            var menuItemProviderTypes = appContext.TypeScanner.FindTypes<IMenuItemProvider>();
            foreach (var type in menuItemProviderTypes)
            {
                var attribute = type.GetAttribute<MenuItemProviderAttribute>(false);
                var registration = builder.RegisterType(type)
                    .As<IMenuItemProvider>()
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