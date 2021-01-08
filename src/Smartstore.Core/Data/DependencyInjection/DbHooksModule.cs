using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Engine;
using Smartstore.DependencyInjection;
using Smartstore.Engine.Modularity;
using Smartstore.Events;

namespace Smartstore.Core.DependencyInjection
{
    internal class DbHooksModule : Autofac.Module
    {
        private readonly IApplicationContext _appContext;

        public DbHooksModule(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultDbHookHandler>()
                .As<IDbHookHandler>()
                .InstancePerLifetimeScope();

            var hookTypes = _appContext.TypeScanner.FindTypes<IDbSaveHook>(ignoreInactiveModules: true);

            foreach (var hookType in hookTypes)
            {
                var types = DiscoverHookTypes(hookType);

                var registration = builder.RegisterType(hookType)
                    .As<IDbSaveHook>()
                    .WithMetadata<HookMetadata>(m =>
                    {
                        m.For(em => em.HookedType, types.EntityType);
                        m.For(em => em.ImplType, hookType);
                        m.For(em => em.DbContextType, types.ContextType ?? typeof(SmartDbContext));
                        m.For(em => em.Important, hookType.HasAttribute<ImportantAttribute>(false));
                        m.For(em => em.Order, hookType.GetAttribute<OrderAttribute>(false)?.Order ?? 0);
                    });

                var lifetime = hookType.GetAttribute<ServiceLifetimeAttribute>(false)?.Lifetime ?? ServiceLifetime.Scoped;
                if (lifetime == ServiceLifetime.Singleton)
                {
                    registration.SingleInstance();
                }
                else if (lifetime == ServiceLifetime.Transient)
                {
                    registration.InstancePerDependency();
                }
                else
                {
                    registration.InstancePerLifetimeScope();
                }

                // Find other interfaces that the impl type implements and override
                // a possibly existing previous registration. E.g.: SettingService
                // also implements IDbSaveHook directly. But we don't want two different registrations,
                // we want Autofac to resolve the same instance of SettingsService, 
                // either injected as ISettingService or IDbSaveHook.
                var interfaces = hookType.GetTypeInfo().ImplementedInterfaces
                    .Where(x => !x.IsGenericType)
                    .Except(EventsModule.IgnoredInterfaces.Concat(new[] { typeof(IDbSaveHook), typeof(IConsumer) }))
                    .ToArray();

                if (interfaces.Length > 0)
                {
                    // This call actually overrides any former registration for the interface.
                    registration.As(interfaces);
                }
            }
        }

        private static (Type ContextType, Type EntityType) DiscoverHookTypes(Type type)
        {
            var x = type.BaseType;
            while (x != null && x != typeof(object))
            {
                if (x.IsGenericType)
                {
                    var gtd = x.GetGenericTypeDefinition();
                    if (gtd == typeof(AsyncDbSaveHook<>))
                    {
                        return (typeof(SmartDbContext), x.GetGenericArguments()[0]);
                    }
                    if (gtd == typeof(AsyncDbSaveHook<,>))
                    {
                        var args = x.GetGenericArguments();
                        return (args[0], args[1]);
                    }
                }

                x = x.BaseType;
            }

            foreach (var intface in type.GetInterfaces())
            {
                if (intface.IsGenericType)
                {
                    var gtd = intface.GetGenericTypeDefinition();
                    if (gtd == typeof(IDbSaveHook<>))
                    {
                        return (intface.GetGenericArguments()[0], typeof(BaseEntity));
                    }
                }
            }

            return (typeof(SmartDbContext), typeof(BaseEntity));
        }
    }
}