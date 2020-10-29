using Autofac;

namespace Smartstore.Core.Localization.DependencyInjection
{
    public sealed class LocalizationModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<LocalizedEntityService>().As<ILocalizedEntityService>().InstancePerLifetimeScope();
        }
    }
}