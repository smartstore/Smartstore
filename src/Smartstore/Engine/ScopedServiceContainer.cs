#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Engine
{
    public sealed class ScopedServiceContainer
    {
        private readonly ILifetimeScopeAccessor _scopeAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILifetimeScope _rootContainer;

        public ScopedServiceContainer(ILifetimeScopeAccessor scopeAccessor, IHttpContextAccessor httpContextAccessor, ILifetimeScope rootContainer)
        {
            Guard.NotNull(scopeAccessor);
            Guard.NotNull(httpContextAccessor);
            Guard.NotNull(rootContainer);

            _scopeAccessor = scopeAccessor;
            _httpContextAccessor = httpContextAccessor;
            _rootContainer = rootContainer;
        }

        public ILifetimeScope RequestContainer
        {
            [DebuggerStepThrough]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _scopeAccessor.LifetimeScope ?? _rootContainer;
            }
        }

        public bool IsHttpRequestScope()
            => _httpContextAccessor.HttpContext != null;

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>() where T : class
            => RequestContainer.Resolve<T>();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? ResolveOptional<T>() where T : class
            => RequestContainer.ResolveOptional<T>();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveKeyed<T>(object key) where T : class
            => RequestContainer.ResolveKeyed<T>(key);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? ResolveOptionalKeyed<T>(object key) where T : class
            => RequestContainer.ResolveOptionalKeyed<T>(key);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveNamed<T>(string name) where T : class
            => RequestContainer.ResolveNamed<T>(name);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? ResolveOptionalNamed<T>(string name) where T : class
            => RequestContainer.ResolveOptionalNamed<T>(name);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type type)
            => RequestContainer.Resolve(type);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ResolveKeyed(object key, Type type)
            => RequestContainer.ResolveKeyed(key, type);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object ResolveNamed(string name, Type type)
            => RequestContainer.ResolveNamed(name, type);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ResolveAll<T>()
            => RequestContainer.Resolve<IEnumerable<T>>().ToArray();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ResolveAllKeyed<T>(object key)
            => RequestContainer.ResolveKeyed<IEnumerable<T>>(key).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ResolveUnregistered<T>() where T : class
            => (T)RequestContainer.ResolveUnregistered(typeof(T));

        public object ResolveUnregistered(Type type)
            => RequestContainer.ResolveUnregistered(type);

        [DebuggerStepThrough]
        public bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object? instance)
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
        public bool TryResolve<T>([MaybeNullWhen(false)] out T? instance)
            where T : class
        {
            instance = default;

            try
            {
                return RequestContainer.TryResolve(out instance);
            }
            catch
            {
                return false;
            }
        }

        public bool IsRegistered(Type serviceType)
            => RequestContainer.IsRegistered(serviceType);

        public object? ResolveOptional(Type serviceType)
            => RequestContainer.ResolveOptional(serviceType);

        public T InjectProperties<T>(T instance) where T : notnull
            => RequestContainer.InjectProperties(instance);

        public T InjectUnsetProperties<T>(T instance) where T : notnull
            => RequestContainer.InjectUnsetProperties(instance);
    }
}
