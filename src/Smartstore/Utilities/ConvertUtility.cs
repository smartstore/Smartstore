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

namespace Smartstore.Utilities;

public static class ConvertUtility
{
    private static readonly Func<string, string> DefaultKeySelector = static key => key;

    // Avoids allocations from chained Replace calls by doing a single pass when needed.
    private static readonly Func<string, string> HtmlAttributeKeySelector = static key =>
    {
        if (key is null || key.Length == 0)
        {
            return string.Empty;
        }

        // Fast path: no work needed
        bool needsRewrite = false;
        for (int i = 0; i < key.Length; i++)
        {
            char c = key[i];
            if (c == '_' || c == '@')
            {
                needsRewrite = true;
                break;
            }
        }

        if (!needsRewrite)
        {
            return key;
        }

        return RewriteHtmlAttributeKey(key);
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryConvert<T>(
        object? value,
        [MaybeNullWhen(false)] out T? convertedValue)
    {
        if (!TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object? result))
        {
            convertedValue = default;
            return false;
        }

        convertedValue = (T)result!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryConvert<T>(
        object? value,
        CultureInfo? culture,
        [MaybeNullWhen(false)] out T? convertedValue)
    {
        if (!TryConvert(value, typeof(T), culture, out object? result))
        {
            convertedValue = default;
            return false;
        }

        convertedValue = (T)result!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryConvert(
        object? value,
        Type to,
        [MaybeNullWhen(false)] out object? convertedValue)
    {
        return TryConvert(value, to, CultureInfo.InvariantCulture, out convertedValue);
    }

    public static bool TryConvert(
        object? value,
        Type to,
        CultureInfo? culture,
        [MaybeNullWhen(false)] out object? convertedValue)
    {
        ArgumentNullException.ThrowIfNull(to);

        // Default.
        convertedValue = null;

        if (value is null || ReferenceEquals(value, DBNull.Value))
        {
            // null is valid for reference types and Nullable<T>.
            // For value types, only string is special-cased (legacy behavior).
            return to == typeof(string) || !to.IsBasicType();
        }

        // Fast path: already assignable.
        if (to == typeof(object) || to.IsInstanceOfType(value))
        {
            convertedValue = value;
            return true;
        }

        culture ??= CultureInfo.InvariantCulture;
        Type from = value.GetType();

        try
        {
            // Prefer converter for destination type (value -> to)
            var converter = TypeConverterFactory.GetConverter(to);
            if (converter is not null && converter.CanConvertFrom(from))
            {
                convertedValue = converter.ConvertFrom(culture, value);
                return true;
            }

            // Try the other way round (to <- from)
            converter = TypeConverterFactory.GetConverter(from);
            if (converter is not null && converter.CanConvertTo(to))
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

        // Pre-size and avoid per-item IDictionary/Add overhead by filling a Dictionary first.
        var dict = new Dictionary<string, object?>(anonymousDictionary.Count, StringComparer.Ordinal);
        foreach (var item in anonymousDictionary)
        {
            dict[item.Key] = item.Value;
        }

        return (ExpandoObject)ToExpandoCore(dict);
    }

    private static ExpandoObject ToExpandoCore(Dictionary<string, object?> source)
    {
        IDictionary<string, object?> expando = new ExpandoObject();
        foreach (var item in source)
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
        // Avoid LINQ allocation/materialization; write directly.
        var dict = ObjectToDictionary(obj, HtmlAttributeKeySelector);
        var result = new Dictionary<string, string?>(dict.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in dict)
        {
            result[kvp.Key] = kvp.Value?.ToString();
        }

        return result;
    }

    /// <summary>
    /// Given an object, adds each instance property with a public get method as a key and its
    /// associated value to a dictionary.
    ///
    /// If the object is already an <see cref="IDictionary{string, object}"> instance, then a copy is returned.
    ///
    /// If the object is a <see cref="JsonObject"/> instance, then it will be converted recursively.
    /// </summary>
    /// <param name="keySelector">Key selector. Not invoked when <paramref name="value"/> is already a dictionary or <see cref="JsonObject"/>.</param>
    /// <param name="deep">When true, converts all nested objects to dictionaries also</param>
    /// <remarks>
    /// The implementation of FastProperty will cache the property accessors per-type. This is
    /// faster when the the same type is used multiple times with ObjectToDictionary.
    /// </remarks>
    public static IDictionary<string, object?> ObjectToDictionary(
        object? value,
        Func<string, string>? keySelector,
        bool deep = false)
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

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            // Avoid JsonNode.Parse(jsonElement.GetRawText()) (allocates big string).
            // Use JsonDocument to enumerate properties without re-serializing.
            return JsonElementObjectToDictionary(jsonElement, deep);
        }

        if (value is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        keySelector ??= DefaultKeySelector;

        // Pre-size dictionary to property count to avoid resizes.
        var props = FastProperty.GetProperties(value.GetType());
        dictionary = new Dictionary<string, object?>(props.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in props)
        {
            object? propValue = kvp.Value.GetValue(value);
            Type propType = kvp.Value.Property.PropertyType;

            if (deep && propValue is not null && (propType == typeof(JsonObject) || propType.IsPlainObjectType()))
            {
                propValue = ObjectToDictionary(propValue, DefaultKeySelector, deep: true);
            }

            dictionary[keySelector(kvp.Value.Name)] = propValue;
        }

        return dictionary;
    }

    private static Dictionary<string, object?> JsonObjectToDictionary(JsonObject value, bool deep)
    {
        var result = new Dictionary<string, object?>(value.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, node) in value)
        {
            result[key] = JsonNodeToObject(node, deep);
        }

        return result;
    }

    private static Dictionary<string, object?> JsonElementObjectToDictionary(JsonElement element, bool deep)
    {
        // Enumerate without creating raw JSON string.
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var prop in element.EnumerateObject())
        {
            result[prop.Name] = JsonElementToObject(prop.Value, deep);
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
            JsonArray arr => JsonArrayToObjectArray(arr),
            _ => node
        };
    }

    private static object? JsonElementToObject(JsonElement el, bool deep)
    {
        if (el.IsNullOrUndefined())
        {
            return null;
        }

        if (!deep)
        {
            // shallow: preserve JsonElement (caller expects JSON-friendly representation)
            return el;
        }

        // deep: convert objects/arrays recursively; scalars to CLR primitives when possible
        if (el.TryGetScalarValue(out var scalar))
        {
            return scalar;
        }

        return el.ValueKind switch
        {
            JsonValueKind.Object => JsonElementObjectToDictionary(el, deep: true),
            JsonValueKind.Array => JsonElementArrayToObjectArray(el),
            _ => el.GetRawText()
        };
    }

    private static object?[] JsonArrayToObjectArray(JsonArray arr)
    {
        // Avoid LINQ allocations.
        var result = new object?[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            result[i] = JsonNodeToObject(arr[i], deep: true);
        }
        return result;
    }

    private static object?[] JsonElementArrayToObjectArray(JsonElement el)
    {
        int len = el.GetArrayLength();
        var result = new object?[len];

        int i = 0;
        foreach (var item in el.EnumerateArray())
        {
            result[i++] = JsonElementToObject(item, deep: true);
        }

        return result;
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

        // Keep behavior, but avoid a second concatenation in the positive case.
        string str = Math.Abs(value).ToString(format, culture);

        if (value < 0)
        {
            str = culture.TextInfo.IsRightToLeft
                ? "&lrm;&minus;" + str
                : "&minus;" + str;
        }

        return new HtmlString(str);
    }

    private static string RewriteHtmlAttributeKey(string key)
    {
        // Single-pass rewrite to avoid creating two intermediate strings.
        // '_' => '-', '@' removed.
        int len = key.Length;
        char[] buffer = new char[len]; // max size; may not fill.

        int j = 0;
        bool changed = false;

        for (int i = 0; i < len; i++)
        {
            char c = key[i];
            if (c == '@')
            {
                changed = true;
                continue;
            }

            char rewritten = c == '_' ? '-' : c;
            if (rewritten != c)
            {
                changed = true;
            }

            buffer[j++] = rewritten;
        }

        return changed ? new string(buffer, 0, j) : key;
    }
}
