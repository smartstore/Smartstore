using Autofac;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Builders;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Bootstrapping;

internal sealed class WidgetStarter : StarterBase
{
    public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
    {
        builder.RegisterType<DefaultWidgetSelector>().As<IWidgetSelector>().InstancePerLifetimeScope();
        builder.RegisterType<DefaultWidgetProvider>().As<IWidgetProvider>().As<IWidgetSource>().InstancePerLifetimeScope();
        builder.RegisterType<PageAssetBuilder>().As<IPageAssetBuilder>().InstancePerLifetimeScope();
        builder.RegisterType<NullAssetTagGenerator>().As<IAssetTagGenerator>().SingleInstance();

        var registration = builder.RegisterType<WidgetService>().As<IWidgetService>().InstancePerLifetimeScope();
        if (appContext.IsInstalled)
        {
            registration.As<IWidgetSource>();
        }

        // View/Widget invokers
        builder.RegisterType<DefaultViewInvoker>().As<IViewInvoker>().InstancePerLifetimeScope();
        builder.RegisterType<ComponentWidgetInvoker>().As<IWidgetInvoker<ComponentWidget>>().SingleInstance();
        builder.RegisterType<PartialViewWidgetInvoker>().As<IWidgetInvoker<PartialViewWidget>>().SingleInstance();

        // Dashboard widgets
        if (appContext.IsInstalled) 
        {
            builder.RegisterType<DashboardService>().As<IDashboardService>().InstancePerLifetimeScope();
            builder.RegisterType<DashboardCssBuilder>().As<IDashboardCssBuilder>().InstancePerLifetimeScope();

            foreach (var type in appContext.TypeScanner.FindTypes<IDashboardWidget>())
            {
                builder.RegisterType(type)
                    .As<IDashboardWidget>()
                    .WithMetadata<DashboardMetadata>(metadata => metadata.For(x => x.SystemName, GetDashboardSystemName(type)))
                    .InstancePerLifetimeScope();
            }

            foreach (var type in appContext.TypeScanner.FindTypes<IDashboardLayoutProvider>())
            {
                builder.RegisterType(type)
                    .As<IDashboardLayoutProvider>()
                    .WithMetadata<DashboardMetadata>(metadata => metadata.For(x => x.SystemName, GetDashboardSystemName(type)))
                    .InstancePerLifetimeScope();
            }
        }
    }

    private static string GetDashboardSystemName(Type type)
    {
        if (!type.TryGetAttribute<SystemNameAttribute>(false, out var attr) || attr.Name.IsEmpty())
        {
            throw new InvalidOperationException(
                $"Dashboard component '{type.FullName}' must declare a SystemNameAttribute.");
        }

        return attr.Name;
    }
}
