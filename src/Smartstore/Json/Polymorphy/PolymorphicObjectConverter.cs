#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Polymorphy;

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
        private readonly PolymorphyOptions _polyOptions;

        public PolymorphicObjectConverter(PolymorphyOptions options) 
            => _polyOptions = options;

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var doc = JsonDocument.ParseValue(ref reader);
            object? obj = PolymorphyCodec.Read(doc.RootElement, typeof(T), options, _polyOptions);
            return (T?)obj;
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            // PolymorphyCodec writes wrapper object with $type first (or null).
            PolymorphyCodec.Write(writer, value, options, _polyOptions);
        }
    }
}