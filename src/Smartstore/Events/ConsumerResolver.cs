using System.Reflection;
using Autofac;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;

namespace Smartstore.Events
{
    public class ConsumerResolver : IConsumerResolver
    {
        private readonly Work<IComponentContext> _container;

        public ConsumerResolver(Work<IComponentContext> container)
        {
            _container = container;
        }

        public virtual IConsumer Resolve(ConsumerDescriptor descriptor)
        {
            if (descriptor.ModuleDescriptor == null || IsActiveForStore(descriptor.ModuleDescriptor))
            {
                return _container.Value.ResolveKeyed<IConsumer>(descriptor.ContainerType);
            }

            return null;
        }

        public virtual object ResolveParameter(ParameterInfo p, IComponentContext c = null)
        {
            return (c ?? _container.Value).Resolve(p.ParameterType);
        }

        private bool IsActiveForStore(ModuleDescriptor module)
        {
            // TODO: (core) Implement ConsumerResolver.IsActiveForStore

            return true;

            //int storeId = 0;
            //if (EngineContext.Current.IsFullyInitialized)
            //{
            //    storeId = _container.Value.Resolve<IStoreContext>().CurrentStore.Id;
            //}

            //if (storeId == 0)
            //{
            //    return true;
            //}

            //var settingService = _container.Value.Resolve<ISettingService>();

            //var limitedToStoresSetting = settingService.GetSettingByKey<string>(plugin.GetSettingKey("LimitedToStores"));
            //if (limitedToStoresSetting.IsEmpty())
            //{
            //    return true;
            //}

            //var limitedToStores = limitedToStoresSetting.ToIntArray();
            //if (limitedToStores.Length > 0)
            //{
            //    var flag = limitedToStores.Contains(storeId);
            //    return flag;
            //}

            //return true;
        }
    }
}