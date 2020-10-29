using Autofac;

namespace Smartstore.Core.Stores.DependencyInjection
{
    public sealed class StoresModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IStoreContext>().As<StoreContext>().InstancePerLifetimeScope();
            builder.RegisterType<IStoreMappingService>().As<StoreMappingService>().InstancePerLifetimeScope();
        }
    }
}