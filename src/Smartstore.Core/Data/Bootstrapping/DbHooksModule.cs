using System.Reflection;
using Autofac;
using Smartstore.Bootstrapping;
using Smartstore.Core.Data;
using Smartstore.Core.Widgets;
using Smartstore.Data.Hooks;
using Smartstore.Engine.Modularity;
using Smartstore.Events;

namespace Smartstore.Core.Bootstrapping
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
            builder.RegisterType<DefaultDbHookRegistry>().As<IDbHookRegistry>().SingleInstance();
            builder.RegisterType<DefaultDbHookActivator>().As<IDbHookActivator>().InstancePerLifetimeScope();
            builder.RegisterType<DefaultDbHookHandler>().As<IDbHookHandler>().InstancePerLifetimeScope();

            var appInstalled = _appContext.IsInstalled;
            var hookTypes = _appContext.TypeScanner.FindTypes<IDbSaveHook>();

            foreach (var hookType in hookTypes)
            {
                var importantAttribute = hookType.GetAttribute<ImportantAttribute>(false);

                // Essential hooks must run during installation
                var shouldRegister = appInstalled || importantAttribute?.Importance == HookImportance.Essential;
                if (!shouldRegister)
                {
                    continue;
                }

                var types = DiscoverHookTypes(hookType);
                
                // Find other interfaces that the impl type implements and override
                // a possibly existing previous registration. E.g.: SettingService
                // also implements IDbSaveHook directly. But we don't want two different registrations,
                // we want Autofac to resolve the same instance of SettingsService, 
                // either injected as ISettingService or IDbSaveHook.
                var serviceTypes = hookType.GetTypeInfo().ImplementedInterfaces
                    .Where(x => !x.IsGenericType)
                    .Except(EventsModule.IgnoredInterfaces.Concat(new[] { typeof(IDbSaveHook), typeof(IConsumer), typeof(IWidgetSource) }))
                    .ToArray();

                var registration = builder.RegisterType(hookType)
                    .As<IDbSaveHook>()
                    .InstancePerAttributedLifetime()
                    .WithMetadata<HookMetadata>(m =>
                    {
                        m.For(em => em.HookedType, types.EntityType);
                        m.For(em => em.ServiceTypes, serviceTypes.Length > 0 ? serviceTypes : new[] { hookType });
                        m.For(em => em.ImplType, hookType);
                        m.For(em => em.DbContextType, types.ContextType ?? typeof(SmartDbContext));
                        m.For(em => em.Importance, importantAttribute?.Importance ?? HookImportance.Normal);
                        m.For(em => em.Order, hookType.GetAttribute<OrderAttribute>(false)?.Order ?? 0);
                    });

                if (serviceTypes.Length > 0)
                {
                    // This call actually overrides any former registration for the interface.
                    registration.As(serviceTypes);
                }
                else
                {
                    registration.AsSelf();
                }
            }
        }

        internal static (Type ContextType, Type EntityType) DiscoverHookTypes(Type type)
        {
            var x = type.BaseType;
            while (x != null && x != typeof(object))
            {
                if (x.IsGenericType)
                {
                    var gtd = x.GetGenericTypeDefinition();
                    if (gtd == typeof(AsyncDbSaveHook<>) || gtd == typeof(DbSaveHook<>))
                    {
                        return (typeof(SmartDbContext), x.GetGenericArguments()[0]);
                    }
                    if (gtd == typeof(AsyncDbSaveHook<,>) || gtd == typeof(DbSaveHook<,>))
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