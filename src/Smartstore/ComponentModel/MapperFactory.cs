#nullable enable

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Engine;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// A static factory that can create type mapper instances (<see cref="IMapper{TFrom, TTo}"/>).
    /// To resolve a mapper instance, use <see cref="GetMapper{TFrom, TTo}(string?)"/>. To map object instances,
    /// call one of the <c>Map*() methods</c> (the corresponding nameless default mapper is resolved internally in this case).
    /// </summary>
    /// <remarks>
    /// <see cref="MapperFactory"/> automatically scans for all concrete <see cref="IMapper{TFrom, TTo}"/> classes 
    /// in all loaded assemblies upon initialization. A mapper is DI-enabled and therefore can depend on any registered service.
    /// If no mapper is found for a specific mapping operation, then a generic mapper is used
    /// which internally delegates object mapping to <see cref="MiniMapper"/>.
    /// </remarks>
    public static class MapperFactory
    {
        // Mapper registrations map
        private static IDictionary<MapperKey, MapperRegistration> _mapperRegistrations = default!;
        // Lifetimes
        private readonly static MapperLifetime _singletonMapperLifetime = new SingletonMapperLifetime();
        private readonly static MapperLifetime _scopeMapperLifetime = new ScopeMapperLifetime();
        private readonly static MapperLifetime _transientMapperLifetime = new TransientMapperLifetime();
        // Init lock
        private readonly static object _lock = new();

        #region Init

        private static void EnsureInitialized()
        {
            if (_mapperRegistrations == null)
            {
                lock (_lock)
                {
                    if (_mapperRegistrations == null)
                    {
                        _mapperRegistrations = new Dictionary<MapperKey, MapperRegistration>();

                        var typeScanner = EngineContext.Current.Application.Services.ResolveOptional<ITypeScanner>();
                        var mapperTypes = typeScanner?.FindTypes(typeof(IMapper<,>));

                        if (mapperTypes != null)
                        {
                            RegisterMappers(mapperTypes.ToArray());
                        }

                    }
                }
            }
        }

        /// <summary>
        /// For testing purposes
        /// </summary>
        internal static void RegisterMappers(params Type[] implTypes)
        {
            var defaultMapperAttribute = new MapperAttribute();

            // Item1 = ImplType, Item2 = declared or default attribute
            var mapperTypes = new Multimap<MapperKey, (Type Type, MapperAttribute Attr)>();

            foreach (var type in implTypes)
            {
                // Extract ALL IMapper<TFrom, TTo> closed types from impl type.
                var closedTypes = type.GetClosedGenericTypesOf(typeof(IMapper<,>));

                // Look up MapperAttribute annotation on impl type or create default.
                var mapperAttr = type.GetAttribute<MapperAttribute>(true) ?? defaultMapperAttribute;

                foreach (var closedType in closedTypes)
                {
                    var args = closedType.GetGenericArguments();

                    var typePair = new MapperKey(
                        args[0],
                        args[1],
                        mapperAttr?.Name.NullEmpty());

                    mapperTypes.Add(typePair, (type, mapperAttr!));
                }
            }

            _mapperRegistrations ??= new Dictionary<MapperKey, MapperRegistration>();

            foreach (var kvp in mapperTypes)
            {
                var orderedMapperTypes = kvp.Value
                    .OrderBy(x => x.Attr.Order)
                    .Select(x => x.Type)
                    .ToArray();

                var lifetimeEnum = kvp.Value
                    .Select(x => x.Attr.Lifetime)
                    .Max();

                var lifetime = lifetimeEnum switch
                {
                    ServiceLifetime.Singleton   => _singletonMapperLifetime,
                    ServiceLifetime.Transient   => _transientMapperLifetime,
                    _                           => _scopeMapperLifetime,
                };

                _mapperRegistrations[kvp.Key] = new MapperRegistration(orderedMapperTypes, lifetime);
            }

            _mapperRegistrations = _mapperRegistrations.ToFrozenDictionary();
        }

        #endregion

        #region GetMapper

        /// <summary>
        /// Gets a mapper implementation for <typeparamref name="TFrom"/> as source and <typeparamref name="TTo"/> as target.
        /// </summary>
        /// <param name="name">Mapper name or <c>null</c> to resolve default mapper.</param>
        /// <returns>The mapper implementation or a generic mapper if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMapper<TFrom, TTo> GetMapper<TFrom, TTo>(string? name = null)
            where TFrom : class
            where TTo : class
            => GetMapperInternal<TFrom, TTo>(name.NullEmpty(), false)!;

        /// <summary>
        /// Gets a mapper implementation for <typeparamref name="TFrom"/> as source and <typeparamref name="TTo"/> as target.
        /// </summary>
        /// <param name="name">Mapper name or <c>null</c> to resolve default mapper.</param>
        /// <returns>The mapper implementation or <c>null</c> if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IMapper<TFrom, TTo>? GetRegisteredMapper<TFrom, TTo>(string? name = null)
            where TFrom : class
            where TTo : class
            => GetMapperInternal<TFrom, TTo>(name.NullEmpty(), true);

        private static IMapper<TFrom, TTo>? GetMapperInternal<TFrom, TTo>(string? name, [NotNullWhen(false)] bool onlyRegisteredMapper)
            where TFrom : class
            where TTo : class
        {
            EnsureInitialized();

            var mapperKey = new MapperKey(typeof(TFrom), typeof(TTo), name);

            if (_mapperRegistrations.TryGetValue(mapperKey, out var registration))
            {
                // Found a registration, try to resolve instance from lifetime.
                var instance = registration.Lifetime.GetMapper(mapperKey);
                if (instance != null)
                {
                    // Mapper instance exists, return.
                    return (IMapper<TFrom, TTo>)instance;
                }

                // No instance yet, must activate.
                var instances = registration.ImplTypes
                    .Select(t => ResolveMapper(t, EngineContext.Current.Scope))
                    .Where(x => x != null)
                    .ToArray();

                if (instances.Length == 1)
                {
                    registration.Lifetime.SetMapper(mapperKey, instances[0]!);
                    return instances[0];
                }
                else if (instances.Length > 1)
                {
                    // Create and return a composite mapper if more than one instance exists.
                    var mapper = new CompositeMapper<TFrom, TTo>(instances!);
                    registration.Lifetime.SetMapper(mapperKey, mapper);
                    return mapper;
                }
            }

            if (onlyRegisteredMapper)
            {
                return null;
            }

            return new GenericMapper<TFrom, TTo>();

            static IMapper<TFrom, TTo>? ResolveMapper(Type mapperType, ScopedServiceContainer? scope)
            {
                var instance = scope?.ResolveUnregistered(mapperType);
                if (instance != null)
                {
                    scope!.InjectProperties(instance);
                    return (IMapper<TFrom, TTo>)instance;
                }

                return null;
            }
        }

        #endregion

        #region Map

        /// <summary>
        /// Maps instance of <typeparamref name="TFrom"/> to instance of <typeparamref name="TTo"/>.
        /// </summary>
        /// <param name="from">Source instance</param>
        /// <param name="parameters">Custom parameters for the underlying mapper.</param>
        /// <returns>The mapped target instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static Task<TTo> MapAsync<TFrom, TTo>(TFrom from, dynamic? parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(from);

            var to = new TTo();
            await GetMapper<TFrom, TTo>().MapAsync(from, to, parameters);
            return to;
        }

        /// <summary>
        /// Maps instance of <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// </summary>
        /// <param name="from">Source instance</param>
        /// <param name="to">Target instance</param>
        /// <param name="parameters">Custom parameters for the underlying mapper.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task MapAsync<TFrom, TTo>(TFrom from, TTo to, dynamic? parameters = null)
            where TFrom : class
            where TTo : class
        {
            return GetMapper<TFrom, TTo>().MapAsync(
                Guard.NotNull(from),
                Guard.NotNull(to),
                parameters);
        }

        /// <summary>
        /// Tries to map instance of <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.
        /// This method will do nothing if no mapper is registered for the given type pair.
        /// </summary>
        /// <param name="from">Source instance</param>
        /// <param name="to">Target instance</param>
        /// <param name="parameters">Custom parameters for the underlying mapper.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task MapWithRegisteredMapperAsync<TFrom, TTo>(TFrom from, TTo to, dynamic? parameters = null)
            where TFrom : class
            where TTo : class
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var mapper = GetRegisteredMapper<TFrom, TTo>();

            if (mapper != null)
            {
                return mapper.MapAsync(from, to, parameters);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<List<TTo>> MapListAsync<TFrom, TTo>(IQueryable<TFrom> from, dynamic? parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(from);
            return await IMapperExtensions.MapListAsync(GetMapper<TFrom, TTo>(), await from.ToListAsync(), parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<List<TTo>> MapListAsync<TFrom, TTo>(IEnumerable<TFrom> from, dynamic? parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            return IMapperExtensions.MapListAsync(GetMapper<TFrom, TTo>(), from, parameters);
        }

        #endregion

        #region Internal nested classes

        class MapperKey : Tuple<Type, Type, string?>
        {
            public MapperKey(Type fromType, Type toType, string? name)
                : base(fromType, toType, name)
            {
            }

            public Type FromType { get => Item1; }
            public Type ToType { get => Item2; }
            public string? Name { get => Item3; }
        }

        readonly struct MapperType
        {
            public MapperType(Type type, int order)
            {
                Type = type; 
                Order = order;
            }

            public Type Type { get; }
            public int Order { get; }
        }

        readonly struct MapperRegistration
        {
            public MapperRegistration(Type[] implTypes, MapperLifetime lifetime)
            {
                ImplTypes = implTypes;
                Lifetime = lifetime;
            }

            public Type[] ImplTypes { get; }
            public MapperLifetime Lifetime { get; }
        }

        abstract class MapperLifetime
        {
            public abstract object? GetMapper(MapperKey key);
            public abstract void SetMapper(MapperKey key, object mapper);
        }

        class TransientMapperLifetime : MapperLifetime
        {
            public override object? GetMapper(MapperKey key) => null;
            public override void SetMapper(MapperKey key, object mapper) { }
        }

        class ScopeMapperLifetime : MapperLifetime
        {
            public override object? GetMapper(MapperKey key) 
                => RequestCache.Get<object>(key);

            public override void SetMapper(MapperKey key, object mapper)
                => RequestCache.Put(key, mapper);

            static IRequestCache RequestCache
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => EngineContext.Current.Scope.ResolveOptional<IRequestCache>() ?? NullRequestCache.Instance;
            }
        }

        class SingletonMapperLifetime : MapperLifetime
        {
            private readonly Dictionary<MapperKey, object> _mapperInstances = new();

            public override object? GetMapper(MapperKey key)
                => _mapperInstances.Get(key);

            public override void SetMapper(MapperKey key, object mapper)
                => _mapperInstances[key] = mapper;
        }

        #endregion
    }
}
