using Autofac;
using Smartstore.Core.Widgets;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
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
        }
    }
}
