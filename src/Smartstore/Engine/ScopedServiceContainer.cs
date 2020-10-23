using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;

namespace Smartstore.Engine
{
    public sealed class ScopedServiceContainer
    {
        private readonly ILifetimeScopeAccessor _scopeAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILifetimeScope _rootContainer;
        private readonly ConcurrentDictionary<Type, FastActivator> _cachedActivators = new ConcurrentDictionary<Type, FastActivator>();

        public ScopedServiceContainer(ILifetimeScopeAccessor scopeAccessor, IHttpContextAccessor httpContextAccessor, ILifetimeScope rootContainer)
        {
            Guard.NotNull(scopeAccessor, nameof(scopeAccessor));
            Guard.NotNull(httpContextAccessor, nameof(httpContextAccessor));
            Guard.NotNull(rootContainer, nameof(rootContainer));

            _scopeAccessor = scopeAccessor;
            _httpContextAccessor = httpContextAccessor;
            _rootContainer = rootContainer;
        }

        private ILifetimeScope RequestContainer
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    return _httpContextAccessor.HttpContext.GetServiceScope();
                }
                
                return _scopeAccessor.LifetimeScope ?? _rootContainer;
            }
        }

        public bool IsHttpRequestScope()
        {
            return _httpContextAccessor.HttpContext != null;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>() where T : class
        {
            return RequestContainer.Resolve<T>();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveOptional<T>() where T : class
        {
            return RequestContainer.ResolveOptional<T>();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveKeyed<T>(object key) where T : class
        {
            return RequestContainer.ResolveKeyed<T>(key);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveOptionalKeyed<T>(object key) where T : class
        {
            return RequestContainer.ResolveOptionalKeyed<T>(key);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveNamed<T>(string name) where T : class
        {
            return RequestContainer.ResolveNamed<T>(name);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveOptionalNamed<T>(string name) where T : class
        {
            return RequestContainer.ResolveOptionalNamed<T>(name);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type type)
        {
            return RequestContainer.Resolve(type);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ResolveKeyed(object key, Type type)
        {
            return RequestContainer.ResolveKeyed(key, type);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ResolveNamed(string name, Type type)
        {
            return RequestContainer.ResolveNamed(name, type);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ResolveAll<T>()
        {
            return RequestContainer.Resolve<IEnumerable<T>>().ToArray();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ResolveAllKeyed<T>(object key)
        {
            return RequestContainer.ResolveKeyed<IEnumerable<T>>(key).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveUnregistered<T>() where T : class
        {
            return ResolveUnregistered(typeof(T)) as T;
        }

        public object ResolveUnregistered(Type type)
        {
            object[] parameterInstances = null;

            if (!_cachedActivators.TryGetValue(type, out FastActivator activator))
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var constructor in constructors)
                {
                    var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    if (TryResolveAll(parameterTypes, out parameterInstances))
                    {
                        activator = new FastActivator(constructor);
                        _cachedActivators.TryAdd(type, activator);
                        break;
                    }
                }
            }

            if (activator != null)
            {
                if (parameterInstances == null)
                {
                    TryResolveAll(activator.ParameterTypes, out parameterInstances);
                }

                if (parameterInstances != null)
                {
                    return activator.Activate(parameterInstances);
                }
            }

            throw new SmartException("No constructor for {0} was found that had all the dependencies satisfied.".FormatInvariant(type.Name.NaIfEmpty()));
        }

        private bool TryResolveAll(Type[] types, out object[] instances, ILifetimeScope scope = null)
        {
            instances = null;

            try
            {
                var instances2 = new object[types.Length];

                for (int i = 0; i < types.Length; i++)
                {
                    var service = Resolve(types[i]);
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
                // TODO: (core) uncomment once logging is properly up and running
                //_container.Resolve<ILoggerFactory>().CreateLogger(this.GetType()).LogError(ex, ex.Message);
                return false;
            }
        }

        [DebuggerStepThrough]
        public bool TryResolve(Type serviceType, out object instance)
        {
            instance = null;

            try
            {
                return RequestContainer.TryResolve(serviceType, out instance);
            }
            catch
            {
                return false;
            }
        }

        [DebuggerStepThrough]
        public bool TryResolve<T>(out T instance)
            where T : class
        {
            instance = default;

            try
            {
                return RequestContainer.TryResolve<T>(out instance);
            }
            catch
            {
                return false;
            }
        }

        public bool IsRegistered(Type serviceType)
        {
            return RequestContainer.IsRegistered(serviceType);
        }

        public object ResolveOptional(Type serviceType)
        {
            return RequestContainer.ResolveOptional(serviceType);
        }

        public T InjectProperties<T>(T instance)
        {
            return RequestContainer.InjectProperties(instance);
        }

        public T InjectUnsetProperties<T>(T instance)
        {
            return RequestContainer.InjectUnsetProperties(instance);
        }
    }

    //// TODO: (core) Do we really need this?
    //public static class ServiceContainerExtensions
    //{
    //    public static IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> WithNullCache<TLimit, TReflectionActivatorData, TStyle>(this IRegistrationBuilder<TLimit, TReflectionActivatorData, TStyle> registration) where TReflectionActivatorData : ReflectionActivatorData
    //    {
    //        return registration.WithParameter(Autofac.Core.ResolvedParameter.ForNamed<ICache>("null"));
    //    }
    //}
}
