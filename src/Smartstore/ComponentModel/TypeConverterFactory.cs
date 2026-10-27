using System.Collections.Concurrent;
using System.Reflection;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.ComponentModel;

public static class TypeConverterFactory
{
    private static readonly List<ITypeConverterProvider> _providers = [];
    private static readonly ConcurrentDictionary<Type, ITypeConverter> _typeConverters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, ITypeConverter> _propertyConverters = new();

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

    /// <summary>
    /// Gets the converter declared on a property, or null if no converter is declared.
    /// Supports both Smartstore and System.ComponentModel converters.
    /// </summary>
    public static ITypeConverter GetConverter(PropertyInfo property)
    {
        Guard.NotNull(property);

        return _propertyConverters.GetOrAdd(property, AttributedTypeConverterProvider.GetAttributedConverter);
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