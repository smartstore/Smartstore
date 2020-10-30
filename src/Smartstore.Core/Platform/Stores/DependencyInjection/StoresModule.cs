using Autofac;

namespace Smartstore.Core.Stores.DependencyInjection
{
    public sealed class StoresModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StoreContext>().As<IStoreContext>().InstancePerLifetimeScope();
            builder.RegisterType<StoreMappingService>().As<IStoreMappingService>().InstancePerLifetimeScope();
        }
    }
}