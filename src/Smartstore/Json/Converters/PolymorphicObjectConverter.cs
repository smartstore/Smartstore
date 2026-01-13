#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Converters;

/// <summary>
/// Polymorphic "slot" converter factory: object, interfaces, and abstract base types.
/// - Reads legacy NSJ Objects: {"$type":"...","Prop":...}
/// - Writes STJ-ish: {"$type":"...","$value":...} for arrays/scalars.
/// </summary>
internal sealed class PolymorphicObjectConverterFactory : JsonConverterFactory
{
    private readonly PolymorphyOptions _options;

    public PolymorphicObjectConverterFactory(PolymorphyOptions options)
        => _options = Guard.NotNull(options);

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(object) || typeToConvert.IsInterface || typeToConvert.IsAbstract;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var convType = typeof(PolymorphicObjectConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(convType, _options)!;
    }

    private sealed class PolymorphicObjectConverter<T> : JsonConverter<T?>
    {
        private readonly PolymorphyOptions _options;

        public PolymorphicObjectConverter(PolymorphyOptions options) 
            => _options = options;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var doc = JsonDocument.ParseValue(ref reader);
            object? obj = PolymorphyCodec.Read(doc.RootElement, typeof(T), options, _options);

            // PolymorphyCodec returns object?; ensure it matches the slot.
            if (obj is null)
                return default;

            if (obj is T typed)
                return typed;

            // This should only happen for object slots (where any type is OK).
            if (typeof(T) == typeof(object))
                return (T)(object)obj;

            throw new JsonException(
                $"Polymorphic deserialization produced '{obj.GetType()}' which is not assignable to '{typeof(T)}'.");

        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            // PolymorphyCodec writes wrapper object with $type first (or null).
            PolymorphyCodec.Write(writer, value, options, _options);
        }
    }
}