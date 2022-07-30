using System.Reflection;
using Autofac;
using Smartstore.Caching;
using Smartstore.Data.Hooks;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Events;

namespace Smartstore.Bootstrapping
{
    public class EventsModule : Autofac.Module
    {
        public readonly static Type[] IgnoredInterfaces = new Type[]
        {
            typeof(IDisposable),
            typeof(IAsyncDisposable),
            typeof(IScopedService)
        };

        private readonly IApplicationContext _appContext;

        public EventsModule(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NullMessageBus>()
                .As<IMessageBus>()
                .SingleInstance();

            builder.RegisterType<EventPublisher>()
                .As<IEventPublisher>()
                .SingleInstance();

            builder.RegisterType<ConsumerRegistry>()
                .As<IConsumerRegistry>()
                .SingleInstance();

            builder.RegisterType<ConsumerResolver>()
                .As<IConsumerResolver>()
                .SingleInstance();

            builder.RegisterType<ConsumerInvoker>()
                .As<IConsumerInvoker>()
                .SingleInstance();

            builder.RegisterType<NullModuleContraint>()
                .As<IModuleConstraint>()
                .SingleInstance();

            DiscoverConsumers(builder);
        }

        private void DiscoverConsumers(ContainerBuilder builder)
        {
            var moduleCatalog = _appContext.ModuleCatalog;

            var consumerTypes = _appContext.TypeScanner.FindTypes(typeof(IConsumer));
            foreach (var consumerType in consumerTypes)
            {
                var registration = builder
                    .RegisterType(consumerType)
                    .As<IConsumer>()
                    .Keyed<IConsumer>(consumerType)
                    .InstancePerAttributedLifetime();

                var moduleDescriptor = moduleCatalog.GetModuleByAssembly(consumerType.Assembly);

                registration.WithMetadata<EventConsumerMetadata>(m =>
                {
                    m.For(em => em.ContainerType, consumerType);
                    m.For(em => em.ModuleDescriptor, moduleDescriptor);
                });

                // Find other interfaces that the impl type implements and override
                // a possibly existing previous registration. E.g.: SettingService
                // also implements IConsumer directly. But we don't want two different registrations,
                // we want Autofac to resolve the same instance of SettingsService, 
                // either injected as ISettingService or IConsumer.
                var interfaces = consumerType.GetTypeInfo().ImplementedInterfaces
                    .Where(x => !x.IsGenericType)
                    .Except(IgnoredInterfaces.Concat(new[] { typeof(IConsumer), typeof(IDbSaveHook) }))
                    .ToArray();

                if (interfaces.Length > 0)
                {
                    // This call actually overrides any former registration for the interface.
                    registration.As(interfaces);
                }
            }
        }
    }
}