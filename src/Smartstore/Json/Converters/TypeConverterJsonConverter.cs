#nullable enable

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.ComponentModel;

namespace Smartstore.Json.Converters;

public sealed class TypeConverterJsonConverter<T> : JsonConverter<T>
{
    private static readonly ITypeConverter _converter = TypeConverterFactory.GetConverter<T>();
    private static readonly bool _isNonNullableValueType =
        typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null;

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(T);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            if (_isNonNullableValueType)
            {
                throw new JsonException($"Cannot convert null to '{typeof(T)}'.");
            } 

            return default!;
        }

        if (_converter is null || !_converter.CanConvertFrom(typeof(string)))
        {
            throw new JsonException($"No usable TypeConverter for '{typeof(T)}'.");
        }   

        var text = ReadScalarAsString(ref reader);

        try
        {
            var obj = _converter.ConvertFrom(CultureInfo.InvariantCulture, text);
            return obj is T t ? t : (T)obj!;
        }
        catch (Exception ex) when (ex is not JsonException)
        {
            throw new JsonException($"TypeDescriptor conversion failed for '{typeof(T)}' from '{text}'.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        if (_converter is null || !_converter.CanConvertTo(typeof(string)))
        {
            throw new JsonException($"No usable TypeConverter for '{typeof(T)}'.");
        }    

        var s = _converter.ConvertTo(value, typeof(string)) as string;
        writer.WriteStringValue(s);
    }

    private static string ReadScalarAsString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,

            // For number/bool we want the raw token text.
            JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False => reader.HasValueSequence
                ? Encoding.UTF8.GetString(reader.ValueSequence.ToArray())
                : Encoding.UTF8.GetString(reader.ValueSpan),

            _ => throw new JsonException(
                $"Unsupported token '{reader.TokenType}' for TypeDescriptor conversion of '{typeof(T)}'.")
        };
    }
}
