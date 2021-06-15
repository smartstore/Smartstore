using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Smartstore.Engine;

namespace Smartstore.ComponentModel
{
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
                foreach (var intface in type.GetInterfaces())
                {
                    intface.IsSubClass(typeof(IMapper<,>), out var impl);
                    var genericArguments = impl.GetGenericArguments();
                    var typePair = new TypePair(genericArguments[0], genericArguments[1]);
                    _mapperTypes.Add(typePair, type);
                }
            }
        }

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

        public static IMapper<TFrom, TTo> GetMapper<TFrom, TTo>()
            where TFrom : class
            where TTo : class
        {
            EnsureInitialized();

            var key = new TypePair(typeof(TFrom), typeof(TTo));

            var implType = _mapperTypes.Get(key);
            if (implType != null)
            {
                var scope = EngineContext.Current.Scope;
                var instance = scope?.ResolveUnregistered(implType);
                if (instance != null)
                {
                    scope.InjectUnsetProperties(instance);
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
