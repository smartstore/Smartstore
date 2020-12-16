using Autofac;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.DependencyInjection
{
    public sealed class SettingsModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterSource(new SettingsSource());

            builder.RegisterType<SettingFactory>().As<ISettingFactory>().SingleInstance();
            builder.RegisterType<SettingService>().As<ISettingService>().InstancePerLifetimeScope();
        }
    }
}