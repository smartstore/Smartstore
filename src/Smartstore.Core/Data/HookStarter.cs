using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Hooks;
using Smartstore.Domain;
using Smartstore.Engine;

namespace Smartstore.Core.Data
{
    public class HookStarter : StarterBase
    {
        internal readonly static Type[] IgnoredInterfaces = new Type[]
        {
            // TODO: (core) add more ignored interfaces (?)
            typeof(IDisposable),
            typeof(IAsyncDisposable),
            typeof(IScopedService)
        };

        public override int Order => int.MaxValue;

        public override void ConfigureServices(IServiceCollection services, IApplicationContext appContext, bool isActiveModule)
        {
            services.AddDbHookHandler();
        }

        public override void ConfigureContainer(ContainerBuilder builder, IApplicationContext appContext, bool isActiveModule)
        {
            builder.RegisterModule(new DbHooksModule(appContext));
        }
    }

    internal class DbHooksModule : Autofac.Module
    {
        private readonly IApplicationContext _appContext;

        public DbHooksModule(IApplicationContext appContext)
        {
            _appContext = appContext;
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

        protected override void Load(ContainerBuilder builder)
        {
            //if (false)
            //{
            //    // TODO: (core) "DataSettings.DatabaseIsInstalled()"
            //    return;
            //}

            var hookTypes = _appContext.TypeScanner.FindTypes<IDbSaveHook>(ignoreInactiveModules: true);

            foreach (var hookType in hookTypes)
            {
                var types = DiscoverHookTypes(hookType);

                var registration = builder.RegisterType(hookType)
                    .InstancePerLifetimeScope()
                    .As<IDbSaveHook>()
                    .WithMetadata<HookMetadata>(m =>
                    {
                        m.For(em => em.HookedType, types.EntityType);
                        m.For(em => em.ImplType, hookType);
                        m.For(em => em.DbContextType, types.ContextType ?? typeof(SmartDbContext));
                        m.For(em => em.Important, hookType.HasAttribute<ImportantAttribute>(false));
                    });

                // Find other interfaces that the impl type implements and override
                // a possibly existing previous registration. E.g.: SettingService
                // also implements IDbSaveHook directly. But we don't want two different registrations,
                // we want Autofac to resolve the same instance of SettingsService, 
                // either injected as ISettingService or IDbSaveHook.
                var interfaces = hookType.GetTypeInfo().ImplementedInterfaces
                    .Where(x => !x.IsGenericType)
                    .Except(HookStarter.IgnoredInterfaces.Concat(new[] { typeof(IDbSaveHook) }))
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
