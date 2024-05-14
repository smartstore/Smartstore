#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using Smartstore.ComponentModel;

namespace Smartstore.Utilities
{
    public static class ConvertUtility
    {
        private readonly static Func<string, string> DefaultKeySelector = new(key => key);
        private readonly static Func<string, string> HtmlAttributeKeySelector = new(key => key.Replace('_', '-').Replace("@", ""));

        public static bool TryConvert<T>(object? value, [MaybeNullWhen(false)] out T? convertedValue)
        {
            convertedValue = default;

            if (TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object? result))
            {
                convertedValue = (T)result!;
                return true;
            }

            return false;
        }

        public static bool TryConvert<T>(object? value, CultureInfo? culture, [MaybeNullWhen(false)] out T? convertedValue)
        {
            convertedValue = default;

            if (TryConvert(value, typeof(T), culture, out object? result))
            {
                convertedValue = (T)result!;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryConvert(object? value, Type to, [MaybeNullWhen(false)] out object? convertedValue)
        {
            return TryConvert(value, to, CultureInfo.InvariantCulture, out convertedValue);
        }

        public static bool TryConvert(object? value, Type to, CultureInfo? culture, [MaybeNullWhen(false)] out object? convertedValue)
        {
            ArgumentNullException.ThrowIfNull(to);

            convertedValue = null;

            if (value == null || value == DBNull.Value)
            {
                return to == typeof(string) || to.IsBasicType() == false;
            }

            if (to == typeof(object) || (to != typeof(object) && to.IsInstanceOfType(value)))
            {
                // Will always succeed
                convertedValue = value;
                return true;
            }

            Type from = value.GetType();

            culture ??= CultureInfo.InvariantCulture;

            try
            {
                // Get a converter for 'to' (value -> to)
                var converter = TypeConverterFactory.GetConverter(to);
                if (converter != null && converter.CanConvertFrom(from))
                {
                    convertedValue = converter.ConvertFrom(culture, value);
                    return true;
                }

                // Try the other way round with a 'from' converter (to <- from)
                converter = TypeConverterFactory.GetConverter(from);
                if (converter != null && converter.CanConvertTo(to))
                {
                    convertedValue = converter.ConvertTo(culture, null, value, to);
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        public static ExpandoObject ToExpando(object value)
        {
            Guard.NotNull(value);

            var anonymousDictionary = HtmlHelper.AnonymousObjectToHtmlAttributes(value);
            IDictionary<string, object?> expando = new ExpandoObject();
            foreach (var item in anonymousDictionary)
            {
                expando.Add(item);
            }

            return (ExpandoObject)expando;
        }

        /// <inheritdoc cref="ObjectToDictionary(object?, Func{string, string}?, bool)" />
        /// <remarks>
        /// This method translates underscores to dashes and removes '@' 
        /// in each source property to comply with HTML attribute spec.
        /// </remarks>
        public static IDictionary<string, object?> ObjectToDictionary(object obj)
        {
            return ObjectToDictionary(obj, HtmlAttributeKeySelector);
        }

        /// <inheritdoc cref="ObjectToDictionary(object?, Func{string, string}?, bool)" />
        /// <remarks>
        /// This method translates underscores to dashes and removes '@' 
        /// in each source property to comply with HTML attribute spec.
        /// </remarks>
        public static IDictionary<string, string?> ObjectToStringDictionary(object obj)
        {
            return ObjectToDictionary(obj, HtmlAttributeKeySelector).ToDictionary(key => key.Key, el => el.Value?.ToString());
        }

        ///  <summary>
        ///  Given an object, adds each instance property with a public get method as a key and its
        ///  associated value to a dictionary.
        /// 
        ///  If the object is already an <see cref="IDictionary{string, object}"> instance, then a copy is returned.
        ///  
        ///  If the object is a <see cref="JObject"/> instance, then it will be converted recursively.
        ///  </summary>
        ///  <param name="keySelector">Key selector. Not invoked when <paramref name="value"/> is already a dictionary or <see cref="JObject"/>.</param>
        ///  <param name="deep">When true, converts all nested objects to dictionaries also</param>
        ///  <remarks>
        ///  The implementation of FastProperty will cache the property accessors per-type. This is
        ///  faster when the the same type is used multiple times with ObjectToDictionary.
        ///  </remarks>
        public static IDictionary<string, object?> ObjectToDictionary(object? value, Func<string, string>? keySelector, bool deep = false)
        {
            if (value is IDictionary<string, object?> dictionary)
            {
                return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
            }

            if (value is JObject jobj)
            {
                return JObjectToDictionary(jobj, deep);
            }

            dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (value != null)
            {
                keySelector ??= DefaultKeySelector;

                foreach (var kvp in FastProperty.GetProperties(value.GetType()))
                {
                    object? propValue = kvp.Value.GetValue(value);
                    Type propType = kvp.Value.Property.PropertyType;

                    if (deep && propValue != null && (propType == typeof(JObject) || propType.IsPlainObjectType()))
                    {
                        propValue = ObjectToDictionary(propValue, DefaultKeySelector, deep: true);
                    }

                    dictionary[keySelector(kvp.Value.Name)] = propValue;
                }
            }

            return dictionary;
        }

        private static IDictionary<string, object?> JObjectToDictionary(JObject value, bool deep)
        {
            var result = value.ToObject<IDictionary<string, object?>>()!;

            if (deep && result.Any(kvp => kvp.Value is JContainer))
            {
                ProcessObjectProperties(result);
                ProcessArrayProperties(result);
            }

            return result;

            void ProcessObjectProperties(IDictionary<string, object?> props)
            {
                var propNames = from property in props 
                                let name = property.Key
                                let value = property.Value
                                where value is JObject
                                select name;
                propNames.Each(x => props[x] = JObjectToDictionary((JObject)props[x]!, deep));
            }

            void ProcessArrayProperties(IDictionary<string, object?> props)
            {
                var propNames = from property in props
                                let name = property.Key
                                let value = property.Value
                                where value is JArray
                                select name;
                propNames.Each(x => props[x] = ToArray((JArray)props[x]!));
            }

            object[] ToArray(JArray array)
            {
                return array.ToObject<object[]>()!.Select(ProcessArrayEntry).ToArray();
            }

            object ProcessArrayEntry(object value)
            {
                if (value is JObject obj)
                {
                    return JObjectToDictionary(obj, deep);
                }
                else if (value is JArray arr)
                {
                    return ToArray(arr);
                }

                return value;
            }
        }
    }
}
