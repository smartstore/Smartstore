using Autofac;
using Smartstore.Core.Theming;
using Smartstore.Engine;
using Smartstore.Engine.Builders;
using Smartstore.Events;
using Smartstore.Web.Razor;
using Smartstore.Web.Theming;

namespace Smartstore.Web.Bootstrapping
{
    internal sealed class ThemingStarter : StarterBase
    {
        public override bool Matches(IApplicationContext appContext)
        {
            return appContext.IsInstalled;
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.Register(x => new DefaultThemeRegistry(x.Resolve<IEventPublisher>(), x.Resolve<IApplicationContext>(), null, true))
                .As<IThemeRegistry>()
                .SingleInstance();

            builder.RegisterType<DefaultThemeFileResolver>().As<IThemeFileResolver>().SingleInstance();
            builder.RegisterType<DefaultThemeVariableService>().As<IThemeVariableService>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultThemeContext>().As<IThemeContext>().InstancePerLifetimeScope();
            builder.RegisterType<RazorViewInvoker>().As<IRazorViewInvoker>().InstancePerLifetimeScope();
        }
    }
}