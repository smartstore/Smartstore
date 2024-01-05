using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smartstore.ComponentModel;

namespace Smartstore
{
    public static class IServiceProviderExtensions
    {
        private readonly static ConcurrentDictionary<Type, FastActivator> _cachedActivators = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifetimeScope AsLifetimeScope(this IServiceProvider serviceProvider)
            => serviceProvider.GetAutofacRoot();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveUnregistered<T>(this IComponentContext scope) where T : class
            => (T)scope.ResolveUnregistered(typeof(T));

        public static object ResolveUnregistered(this IComponentContext scope, Type type)
        {
            Guard.NotNull(scope);
            Guard.NotNull(type);

            object[] parameterInstances = null;

            var activator = _cachedActivators.GetOrAdd(type, (key) =>
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var constructor in constructors)
                {
                    var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    if (TryResolveAll(scope, parameterTypes, out parameterInstances))
                    {
                        return new FastActivator(constructor);
                    }
                }
                
                return null;
            });

            if (activator != null)
            {
                if (parameterInstances == null)
                {
                    TryResolveAll(scope, activator.ParameterTypes, out parameterInstances);
                }

                if (parameterInstances != null)
                {
                    var instance = activator.Activate(parameterInstances);
                    if (instance != null)
                    {
                        scope.InjectProperties(instance);
                        return instance;
                    }
                }
            }

            throw new InvalidOperationException("No constructor for {0} was found that had all the dependencies satisfied.".FormatInvariant(type?.Name.NaIfEmpty()));
        }

        private static bool TryResolveAll(IComponentContext scope, Type[] types, out object[] instances)
        {
            instances = null;

            try
            {
                var instances2 = new object[types.Length];

                for (int i = 0; i < types.Length; i++)
                {
                    var service = scope.ResolveOptional(types[i]);
                    if (service == null)
                    {
                        return false;
                    }

                    instances2[i] = service;
                }

                instances = instances2;
                return true;
            }
            catch (Exception ex)
            {
                scope.Resolve<ILoggerFactory>().CreateLogger(typeof(IServiceProviderExtensions)).LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
