using System.Collections.Concurrent;

namespace Smartstore.ComponentModel.TypeConverters;

public class CollectionTypeConverterProvider : ITypeConverterProvider
{
    // Cache the constructed generic converter Type per element type.
    // (TypeConverterFactory already caches the resulting converter instance per *sequence type*,
    // but this avoids repeated MakeGenericType work during discovery for many different sequence types.)
    private static readonly ConcurrentDictionary<Type, Type> _converterTypeCache = new();

    public ITypeConverter GetConverter(Type type)
    {
        if (!type.IsEnumerableType(out var elementType))
        {
            return null;
        }

        try
        {
            var converterType = _converterTypeCache.GetOrAdd(
                elementType,
                static t => typeof(EnumerableConverter<>).MakeGenericType(t));

            return (ITypeConverter)Activator.CreateInstance(converterType, args: [type]);
        }
        catch
        {
            return null;
        }
    }
}
