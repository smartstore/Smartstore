using Autofac;
using Smartstore.Core.Configuration;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class SettingsStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext)
        {
            builder.RegisterSource(new SettingsSource());

            builder.RegisterType<SettingFactory>().As<ISettingFactory>().SingleInstance();
            builder.RegisterType<SettingService>().As<ISettingService>().InstancePerLifetimeScope();
        }
    }
}