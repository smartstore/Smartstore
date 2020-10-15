using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Events;
using Smartstore.Engine;

namespace Smartstore.Core.Events
{
    public class EventStarter : StarterBase
    {
        public override int Order => int.MaxValue - 1;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddEventPublisher();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new EventModule(appContext));
        }
    }

    internal class EventModule : Autofac.Module
    {
        private readonly IApplicationContext _appContext;

        public EventModule(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var moduleCatalog = _appContext.ModuleCatalog;

            var consumerTypes = _appContext.TypeScanner.FindTypes(typeof(IConsumer));
            foreach (var consumerType in consumerTypes)
            {
                var registration = builder
                    .RegisterType(consumerType)
                    .As<IConsumer>()
                    .Keyed<IConsumer>(consumerType)
                    .InstancePerLifetimeScope();
               
                var moduleDescriptor = moduleCatalog.GetModuleByAssembly(consumerType.Assembly);
                var isActive = moduleCatalog.IsActiveModuleAssembly(consumerType.Assembly);

                registration.WithMetadata<EventConsumerMetadata>(m =>
                {
                    m.For(em => em.IsActive, isActive);
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
                    .Except(HookStarter.IgnoredInterfaces.Concat(new[] { typeof(IConsumer) }))
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
