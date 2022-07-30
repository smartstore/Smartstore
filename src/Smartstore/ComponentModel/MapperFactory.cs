using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Smartstore.Engine;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// A static factory that can create type mapper instances (<see cref="IMapper{TFrom, TTo}"/>).
    /// To resolve a mapper instance, use <see cref="GetMapper{TFrom, TTo}"/>. To map object instances,
    /// call one of the <c>Map*() methods</c> (the corresponding mapper is resolved internally in this case).
    /// </summary>
    /// <remarks>
    /// <see cref="MapperFactory"/> automatically scans for all concrete <see cref="IMapper{TFrom, TTo}"/> classes 
    /// in all loaded assemblies upon initialization. A mapper is DI-enabled and therefore can depend on any registered service.
    /// If no mapper is found for a specific mapping operation, then a generic mapper is used
    /// which internally delegates object mapping to <see cref="MiniMapper"/>.
    /// </remarks>
    public static class MapperFactory
    {
        private static IDictionary<TypePair, Type> _mapperTypes = null;
        private readonly static object _lock = new();

        private static void EnsureInitialized()
        {
            if (_mapperTypes == null)
            {
                lock (_lock)
                {
                    if (_mapperTypes == null)
                    {
                        _mapperTypes = new Dictionary<TypePair, Type>();

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
        internal static void RegisterMappers(params Type[] mapperTypes)
        {
            if (_mapperTypes == null)
            {
                _mapperTypes = new Dictionary<TypePair, Type>();
            }

            foreach (var type in mapperTypes)
            {
                var closedTypes = type.GetClosedGenericTypesOf(typeof(IMapper<,>));
                foreach (var closedType in closedTypes)
                {
                    var args = closedType.GetGenericArguments();
                    var typePair = new TypePair(args[0], args[1]);
                    _mapperTypes.Add(typePair, type);
                }
            }
        }

        /// <summary>
        /// Maps instance of <typeparamref name="TFrom"/> to instance of <typeparamref name="TTo"/>.
        /// </summary>
        /// <param name="from">Source instance</param>
        /// <param name="parameters">Custom parameters for the underlying mapper.</param>
        /// <returns>The mapped target instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async static Task<TTo> MapAsync<TFrom, TTo>(TFrom from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(from, nameof(from));

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
        public static Task MapAsync<TFrom, TTo>(TFrom from, TTo to, dynamic parameters = null)
            where TFrom : class
            where TTo : class
        {
            return GetMapper<TFrom, TTo>().MapAsync(
                Guard.NotNull(from, nameof(from)),
                Guard.NotNull(to, nameof(to)),
                parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<List<TTo>> MapListAsync<TFrom, TTo>(IQueryable<TFrom> from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            Guard.NotNull(from, nameof(from));
            return await IMapperExtensions.MapListAsync(GetMapper<TFrom, TTo>(), await from.ToListAsync(), parameters);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<List<TTo>> MapListAsync<TFrom, TTo>(IEnumerable<TFrom> from, dynamic parameters = null)
            where TFrom : class
            where TTo : class, new()
        {
            return IMapperExtensions.MapListAsync(GetMapper<TFrom, TTo>(), from, parameters);
        }

        /// <summary>
        /// Gets a mapper implementation for <typeparamref name="TFrom"/> as source and <typeparamref name="TTo"/> as target.
        /// </summary>
        /// <returns>The mapper implementation or a generic mapper if not found.</returns>
        public static IMapper<TFrom, TTo> GetMapper<TFrom, TTo>()
            where TFrom : class
            where TTo : class
        {
            EnsureInitialized();

            var key = new TypePair(typeof(TFrom), typeof(TTo));

            if (_mapperTypes.TryGetValue(key, out var mapperType))
            {
                var scope = EngineContext.Current.Scope;
                var instance = scope?.ResolveUnregistered(mapperType);
                if (instance != null)
                {
                    scope.InjectProperties(instance);
                    return (IMapper<TFrom, TTo>)instance;
                }
            }

            return new GenericMapper<TFrom, TTo>();
        }

        class TypePair : Tuple<Type, Type>
        {
            public TypePair(Type fromType, Type toType)
                : base(fromType, toType)
            {
            }

            public Type FromType { get => base.Item1; }
            public Type ToType { get => base.Item2; }
        }

        class GenericMapper<TFrom, TTo> : Mapper<TFrom, TTo>
            where TFrom : class
            where TTo : class
        {
            protected override void Map(TFrom from, TTo to, dynamic parameters = null)
                => MiniMapper.Map(from, to);
        }
    }
}
