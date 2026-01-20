#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Converters;

/// <summary>
/// System.Text.Json converter for <see cref="Type"/>.
/// Serializes to the assembly-qualified name without version 
/// and deserializes via <see cref="Type.GetType(string)"/>.
/// </summary>
public sealed class TypeJsonConverter : JsonConverter<Type?>
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(Type);

    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Cannot convert token '{reader.TokenType}' to '{typeof(Type)}'.");

        var typeName = reader.GetString();
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        // Behave like Newtonsoft.Json's default Type handling: resolve from an assembly-qualified name.
        // We do not throw if the type cannot be resolved.
        return Type.GetType(typeName, throwOnError: false);
    }

    public override void Write(Utf8JsonWriter writer, Type? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.AssemblyQualifiedNameWithoutVersion());
    }
}
