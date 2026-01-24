#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Routing;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel.TypeConverters;

internal class DictionaryTypeConverter<T> : DefaultTypeConverter where T : IDictionary<string, object>
{
    private static class Cache
    {
        internal static readonly Type ToType = typeof(T);

        internal static readonly bool ToIsRouteValueDictionary = ToType == typeof(RouteValueDictionary);
        internal static readonly bool ToIsDictionary = ToType == typeof(Dictionary<string, object>);
        internal static readonly bool ToIsExpandoObject = ToType == typeof(ExpandoObject);
        internal static readonly bool ToIsHybridExpando = ToType == typeof(HybridExpando);
        internal static readonly bool ToIsFrozenDictionary = ToType == typeof(FrozenDictionary<string, object>);

        private static readonly ConcurrentDictionary<Type, MethodInfo> CreateSequenceActivatorMethodCache = new();

        internal static MethodInfo GetCreateSequenceActivatorMethod(Type elemType)
        {
            return CreateSequenceActivatorMethodCache.GetOrAdd(elemType, static t =>
            {
                return typeof(EnumerableConverter<>).MakeGenericType(t)
                    .GetMethod("CreateSequenceActivator", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException(
                        $"EnumerableConverter<{t.Name}>.CreateSequenceActivator(Type) not found.");
            });
        }
    }

    public DictionaryTypeConverter()
        : base(typeof(object))
    {
    }

    public override bool CanConvertFrom(Type type)
    {
        // A dictionary can be created from JsonObject, POCO, and anonymous types
        return type == typeof(JsonObject)
            || type.IsPlainObjectType()
            || type.IsAnonymousType();
    }

    public override bool CanConvertTo(Type type)
    {
        // A dictionary can be converted to POCO types with default ctor.
        return type.IsPlainObjectType() && type.HasDefaultConstructor();
    }

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        // Obj > Dict
        var dict = ConvertUtility.ObjectToDictionary(value);

        if (Cache.ToIsRouteValueDictionary)
        {
            return new RouteValueDictionary(dict);
        }
        if (Cache.ToIsDictionary)
        {
            return (Dictionary<string, object>)dict;
        }
        if (Cache.ToIsExpandoObject)
        {
            var expando = new ExpandoObject();
            expando.Merge(dict);
            return expando;
        }
        if (Cache.ToIsHybridExpando)
        {
            var expando = new HybridExpando();
            expando.Merge(dict);
            return expando;
        }
        if (Cache.ToIsFrozenDictionary)
        {
            return dict.ToFrozenDictionary();
        }

        return dict;
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        // Dict > Obj
        if (value is IDictionary<string, object> dict)
        {
            var target = Activator.CreateInstance(to);
            if (target != null)
            {
                Populate(dict, target);
                return target;
            }
        }

        return base.ConvertTo(culture, format, value, to);
    }

    private void Populate(IDictionary<string, object> source, object target, params object[] populated)
    {
        var props = FastProperty.GetProperties(target.GetType());

        foreach (var kvp in props)
        {
            var pi = kvp.Value.Property;

            if (source.TryGetValue(pi.Name, out var value))
            {
                if (pi.PropertyType.IsAssignableFrom(value?.GetType()))
                {
                    SetProperty(target, pi, value);
                }
                else if (value is IDictionary<string, object> dict && !pi.PropertyType.IsBasicType())
                {
                    var nestedTarget = pi.GetValue(target);
                    if (nestedTarget == null && CanConvertTo(pi.PropertyType))
                    {
                        nestedTarget = Activator.CreateInstance(pi.PropertyType);
                    }

                    if (nestedTarget != null)
                    {
                        Populate(dict, nestedTarget, populated);
                        SetProperty(target, pi, nestedTarget);
                    }
                }
                else
                {
                    SetProperty(target, pi, value);
                }
            }
            else
            {
                if (pi.PropertyType.IsSequenceType(out var elementType)
                    && !pi.PropertyType.IsDictionaryType()
                    && CanConvertTo(elementType))
                {
                    SetProperty(target, pi, ConvertEnumerable(source, pi, elementType));
                }
            }
        }
    }

    private static void SetProperty(object instance, PropertyInfo pi, object? value)
    {
        if (!pi.CanWrite)
        {
            return;
        }

        if (ConvertUtility.TryConvert(value, pi.PropertyType, CultureInfo.CurrentCulture, out var converted))
        {
            pi.SetValue(instance, converted);
        }
    }

    private static object ConvertEnumerable(IDictionary<string, object> source, PropertyInfo enumerableProp, Type elemType)
    {
        var anyValuesFound = true;
        var index = 0;

        var elements = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType))!;
        var properties = FastProperty.GetProperties(elemType);
        var prefix = enumerableProp.Name;

        while (anyValuesFound)
        {
            object? curElement = null;
            anyValuesFound = false;

            foreach (var kvp in properties)
            {
                var pi = kvp.Value.Property;
                var key = prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]." + pi.Name;

                if (source.TryGetValue(key, out var value))
                {
                    anyValuesFound = true;

                    if (curElement == null)
                    {
                        curElement = Activator.CreateInstance(elemType)!;
                        elements.Add(curElement);
                    }

                    SetProperty(curElement, pi, value);
                }
            }

            index++;
        }

        var createActivatorMethod = Cache.GetCreateSequenceActivatorMethod(elemType);
        var activator = createActivatorMethod.Invoke(null, [enumerableProp.PropertyType])!;
        return activator.GetType().GetMethod("Invoke")!.Invoke(activator, [elements])!;
    }
}
