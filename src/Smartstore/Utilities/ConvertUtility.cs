#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Json;

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
        public static IDictionary<string, object?> ObjectToDictionary(object? obj)
        {
            return ObjectToDictionary(obj, HtmlAttributeKeySelector);
        }

        /// <inheritdoc cref="ObjectToDictionary(object?, Func{string, string}?, bool)" />
        /// <remarks>
        /// This method translates underscores to dashes and removes '@' 
        /// in each source property to comply with HTML attribute spec.
        /// </remarks>
        public static IDictionary<string, string?> ObjectToStringDictionary(object? obj)
        {
            return ObjectToDictionary(obj, HtmlAttributeKeySelector).ToDictionary(key => key.Key, el => el.Value?.ToString());
        }

        ///  <summary>
        ///  Given an object, adds each instance property with a public get method as a key and its
        ///  associated value to a dictionary.
        /// 
        ///  If the object is already an <see cref="IDictionary{string, object}"> instance, then a copy is returned.
        ///  
        ///  If the object is a <see cref="JsonObject"/> instance, then it will be converted recursively.
        ///  </summary>
        ///  <param name="keySelector">Key selector. Not invoked when <paramref name="value"/> is already a dictionary or <see cref="JsonObject"/>.</param>
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

            if (value is JsonObject jsonObj)
            {
                return JsonObjectToDictionary(jsonObj, deep);
            }

            if (value is JsonNode jsonNode)
            {
                // Best effort: treat objects as dictionary, everything else as empty/default.
                if (jsonNode is JsonObject obj)
                {
                    return JsonObjectToDictionary(obj, deep);
                }
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    var node = JsonNode.Parse(jsonElement.GetRawText());
                    if (node is JsonObject obj)
                    {
                        return JsonObjectToDictionary(obj, deep);
                    }
                }
            }

            dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (value != null)
            {
                keySelector ??= DefaultKeySelector;

                foreach (var kvp in FastProperty.GetProperties(value.GetType()))
                {
                    object? propValue = kvp.Value.GetValue(value);
                    Type propType = kvp.Value.Property.PropertyType;

                    if (deep && propValue != null && (propType == typeof(JsonObject) || propType.IsPlainObjectType()))
                    {
                        propValue = ObjectToDictionary(propValue, DefaultKeySelector, deep: true);
                    }

                    dictionary[keySelector(kvp.Value.Name)] = propValue;
                }
            }

            return dictionary;
        }

        private static Dictionary<string, object?> JsonObjectToDictionary(JsonObject value, bool deep)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var (key, node) in value)
            {
                result[key] = JsonNodeToObject(node, deep);
            }

            return result;
        }

        private static object? JsonNodeToObject(JsonNode? node, bool deep)
        {
            if (node is null)
            {
                return null;
            }

            if (node is JsonValue jv)
            {
                // Prefer JsonElement-backed values (common after JsonNode.Parse)
                if (jv.TryGetValue<JsonElement>(out var el))
                {
                    if (el.IsNullOrUndefined())
                    {
                        return null;
                    }

                    // deep: convert scalar values; non-scalars (rare here) fall back to raw json text.
                    if (deep)
                    {
                        return el.TryGetScalarValue(out var value)
                            ? value
                            : el.GetRawText();
                    }

                    // shallow: preserve original JSON representation
                    return (object)el;
                }

                return jv.GetValue<object?>();
            }

            if (!deep)
            {
                // Keep JsonObject/JsonArray as-is (shallow mode)
                return node;
            }

            return node switch
            {
                JsonObject obj => JsonObjectToDictionary(obj, deep: true),
                JsonArray arr => arr.Select(n => JsonNodeToObject(n, deep: true)).ToArray(),
                _ => node
            };
        }

        /// <summary>
        /// Converts <paramref name="value"/> to a string representation suitable for HTML display, 
        /// including a minus sign (&minus;) if the value is negative.
        /// </summary>
        /// <param name="value">Value to cvonvert.</param>
        /// <param name="format">A standard or custom numeric format.</param>
        /// <param name="culture">
        /// Supplies culture-specific formatting information.
        /// <see cref="CultureInfo.CurrentCulture"/> if <c>null</c>.
        /// </param>
        /// <returns>A string representation of <paramref name="value"/> suitable for HTML display.</returns>
        public static IHtmlContent ToHtmlDisplayString(int value, string format = "N0", CultureInfo? culture = null)
        {
            culture ??= CultureInfo.CurrentCulture;

            var str = Math.Abs(value).ToString(format, culture);

            if (value < 0)
            {
                str = (culture.TextInfo.IsRightToLeft ? "&lrm;&minus;" : "&minus;") + str;
            }

            return new HtmlString(str);
        }
    }
}
