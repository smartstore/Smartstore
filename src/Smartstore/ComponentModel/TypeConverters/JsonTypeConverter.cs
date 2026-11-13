using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Smartstore.Json;

namespace Smartstore.ComponentModel.TypeConverters;

/// <summary>
/// Converts values of <typeparamref name="T"/> to and from JSON strings.
/// </summary>
/// <typeparam name="T">The serialized value type.</typeparam>
public class JsonTypeConverter<T> : DefaultTypeConverter
{
    /// <summary>
    /// Initializes a converter for <typeparamref name="T"/>.
    /// </summary>
    public JsonTypeConverter()
        : base(typeof(T))
    {
    }

    public override bool CanConvertFrom(Type type)
        => type == typeof(string);

    public override bool CanConvertTo(Type type)
        => type == typeof(string);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value != null && value is not string)
        {
            return base.ConvertFrom(culture, value);
        }

        var json = value as string;
        var result = json.HasValue()
            ? JsonSerializer.Deserialize(json, typeof(T), SmartJsonOptions.Default)
            : null;

        // Let the configured JSON contract distinguish sequences from dictionaries, strings
        // and custom scalar representations. Deserializing [] also lets the serializer choose
        // the concrete collection and comparer, or reject a type it cannot populate.
        if (result == null && SmartJsonOptions.Default.GetTypeInfo(typeof(T)).Kind == JsonTypeInfoKind.Enumerable)
        {
            return JsonSerializer.Deserialize("[]", typeof(T), SmartJsonOptions.Default);
        }

        // Byte arrays have a Base64 JSON contract rather than an enumerable contract.
        // Preserve their existing empty-input behavior without treating other scalars as sequences.
        return result == null && typeof(T) == typeof(byte[]) ? Array.Empty<byte>() : result;
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        if (to != typeof(string))
        {
            return base.ConvertTo(culture, format, value, to);
        }

        if (value == null || typeof(T) == typeof(byte[]) && value is byte[] { Length: 0 })
        {
            return string.Empty;
        }

        // Serialize once instead of probing an enumerable before serialization: a source may
        // only support one traversal. Inspect the JSON result to normalize empty sequences.
        var json = JsonSerializer.Serialize(value, typeof(T), SmartJsonOptions.Default);
        return json == "[]" && SmartJsonOptions.Default.GetTypeInfo(typeof(T)).Kind == JsonTypeInfoKind.Enumerable
            ? string.Empty
            : json;
    }
}
