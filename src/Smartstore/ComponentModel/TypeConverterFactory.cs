using System.Collections.Concurrent;
using System.Dynamic;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.ComponentModel
{
    public static class TypeConverterFactory
    {
        private static readonly ConcurrentDictionary<Type, ITypeConverter> _typeConverters = new();

        static TypeConverterFactory()
        {
            CreateDefaultConverters();
        }

        private static void CreateDefaultConverters()
        {
            _typeConverters.TryAdd(typeof(DateTime), new DateTimeConverter());
            _typeConverters.TryAdd(typeof(TimeSpan), new TimeSpanConverter());
            _typeConverters.TryAdd(typeof(bool), new BooleanConverter(
                new[] { "yes", "y", "on", "wahr" },
                new[] { "no", "n", "off", "falsch" }));

            var converter = new DictionaryTypeConverter<IDictionary<string, object>>();
            _typeConverters.TryAdd(typeof(IDictionary<string, object>), converter);
            _typeConverters.TryAdd(typeof(Dictionary<string, object>), converter);
            _typeConverters.TryAdd(typeof(RouteValueDictionary), new DictionaryTypeConverter<RouteValueDictionary>());
            _typeConverters.TryAdd(typeof(ExpandoObject), new DictionaryTypeConverter<ExpandoObject>());
            _typeConverters.TryAdd(typeof(HybridExpando), new DictionaryTypeConverter<HybridExpando>());
            _typeConverters.TryAdd(typeof(JObject), new JObjectConverter());
        }

        public static IReadOnlyCollection<ITypeConverter> Converters
        {
            get { return _typeConverters.Values.AsReadOnly(); }
        }

        public static void RegisterConverter<T>(ITypeConverter typeConverter)
        {
            Guard.NotNull(typeConverter, nameof(typeConverter));

            _typeConverters.TryAdd(typeof(T), typeConverter);
        }

        public static void RegisterListConverter<T>(ITypeConverter typeConverter)
        {
            Guard.NotNull(typeConverter, nameof(typeConverter));

            _typeConverters.TryAdd(typeof(List<T>), typeConverter);
            _typeConverters.TryAdd(typeof(IList<T>), typeConverter);
            _typeConverters.TryAdd(typeof(ICollection<T>), typeConverter);
            _typeConverters.TryAdd(typeof(IReadOnlyCollection<T>), typeConverter);
            _typeConverters.TryAdd(typeof(IEnumerable<T>), typeConverter);
            _typeConverters.TryAdd(typeof(T[]), typeConverter);
        }

        public static void RegisterConverter(Type type, ITypeConverter typeConverter)
        {
            Guard.NotNull(type, nameof(type));
            Guard.NotNull(typeConverter, nameof(typeConverter));

            _typeConverters.TryAdd(type, typeConverter);
        }

        public static ITypeConverter RemoveConverter<T>()
        {
            return RemoveConverter(typeof(T));
        }

        public static ITypeConverter RemoveConverter(Type type)
        {
            Guard.NotNull(type, nameof(type));

            _typeConverters.TryRemove(type, out var converter);
            return converter;
        }

        public static ITypeConverter GetConverter<T>()
        {
            return GetConverter(typeof(T));
        }

        public static ITypeConverter GetConverter(object component)
        {
            Guard.NotNull(component, nameof(component));

            return GetConverter(component.GetType());
        }

        public static ITypeConverter GetConverter(Type type)
        {
            Guard.NotNull(type, nameof(type));

            return _typeConverters.GetOrAdd(type, Get);

            ITypeConverter Get(Type t)
            {
                // TypeConverterAttribute
                var attr = type.GetAttribute<System.ComponentModel.TypeConverterAttribute>(false);
                if (attr != null && attr.ConverterTypeName.HasValue())
                {
                    try
                    {
                        var converterType = Type.GetType(attr.ConverterTypeName);
                        if (typeof(ITypeConverter).IsAssignableFrom(converterType))
                        {
                            if (!converterType.HasDefaultConstructor())
                            {
                                throw new SmartException("A type converter specified by attribute must have a default parameterless constructor.");
                            }

                            return (ITypeConverter)Activator.CreateInstance(converterType);
                        }
                    }
                    catch { }
                }

                // Nullable types
                if (type.IsNullable(out Type elementType))
                {
                    return new NullableConverter(type, elementType);
                }

                // Sequence types
                if (type.IsSequenceType(out elementType))
                {
                    var converter = (ITypeConverter)Activator.CreateInstance(typeof(EnumerableConverter<>).MakeGenericType(elementType), type);
                    return converter;
                }

                // Default fallback
                return new DefaultTypeConverter(type);
            }
        }
    }
}
