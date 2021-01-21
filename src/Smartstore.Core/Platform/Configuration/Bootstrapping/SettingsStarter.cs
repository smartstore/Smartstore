using Autofac;
using Smartstore.Core.Configuration;
using Smartstore.Engine;
using Smartstore.Engine.Builders;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class SettingsStarter : StarterBase
    {
        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterSource(new SettingsSource());

            builder.RegisterType<SettingFactory>().As<ISettingFactory>().SingleInstance();
            builder.RegisterType<SettingService>().As<ISettingService>().InstancePerLifetimeScope();
        }
    }
}