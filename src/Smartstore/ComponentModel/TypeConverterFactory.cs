using System.Collections.Concurrent;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.ComponentModel;

public static class TypeConverterFactory
{
    private static readonly List<ITypeConverterProvider> _providers = [];
    private static readonly ConcurrentDictionary<Type, ITypeConverter> _typeConverters = new();

    static TypeConverterFactory()
    {
        CreateDefaultProviders();
    }

    private static void CreateDefaultProviders()
    {
        _providers.Add(new AttributedTypeConverterProvider());
        _providers.Add(new SimpleTypeConverterProvider());
        _providers.Add(new DictionaryTypeConverterProvider());
        _providers.Add(new CollectionTypeConverterProvider());
    }

    public static IList<ITypeConverterProvider> Providers
    {
        get { return _providers; }
    }

    public static ITypeConverter GetConverter<T>()
    {
        return GetConverter(typeof(T));
    }

    public static ITypeConverter GetConverter(object component)
    {
        Guard.NotNull(component);

        return GetConverter(component.GetType());
    }

    internal static ITypeConverter GetConverter(FastProperty fromProperty, FastProperty toProperty, out bool convertTo)
    {
        convertTo = false;
        var fromType = fromProperty.Property.PropertyType;
        var toType = toProperty.Property.PropertyType;

        if (fromType == toType)
        {
            return null;
        }

        if (fromType == typeof(string) && fromProperty.Property.HasAttribute<JsonSerializedPropertyAttribute>(true))
        {
            return new JsonTypeConverter(toType);
        }

        if (toType == typeof(string) && toProperty.Property.HasAttribute<JsonSerializedPropertyAttribute>(true))
        {
            convertTo = true;
            return new JsonTypeConverter(fromType);
        }

        return null;
    }

    public static ITypeConverter GetConverter(Type type)
    {
        Guard.NotNull(type);

        return _typeConverters.GetOrAdd(type, Get);

        ITypeConverter Get(Type t)
        {
            for (var i = 0; i < _providers.Count; i++)
            {
                var converter = _providers[i].GetConverter(t);
                if (converter != null)
                {
                    return converter;
                }
            }

            // Default fallback
            return new DefaultTypeConverter(type);
        }
    }
}