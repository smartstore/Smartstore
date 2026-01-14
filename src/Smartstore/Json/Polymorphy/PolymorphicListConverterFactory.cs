#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Polymorphy;

internal sealed class PolymorphicListConverterFactory : JsonConverterFactory
{
    private readonly PolymorphyOptions _poly;

    public PolymorphicListConverterFactory(PolymorphyOptions poly)
        => _poly = Guard.NotNull(poly);

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsSequenceType();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        typeToConvert.IsSequenceType(out var elementType);

        var convType = typeof(ListConverter<,>).MakeGenericType(typeToConvert, elementType!);
        return (JsonConverter)Activator.CreateInstance(convType, _poly)!;
    }

    private sealed class ListConverter<TList, TElement> : JsonConverter<TList>
    {
        private readonly PolymorphyOptions _poly;

        public ListConverter(PolymorphyOptions poly)
            => _poly = poly;

        public override TList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var doc = JsonDocument.ParseValue(ref reader);
            var el = doc.RootElement;

            // Accept:
            // - wrapped list/array: {"$type":"...","$values":[...]}
            // - raw array: [...]
            JsonElement arrayEl = el;

            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty(_poly.TypePropertyName, out var tp) &&
                tp.ValueKind == JsonValueKind.String)
            {
                // If wrapped, enforce assignability, but still read from $values.
                var runtimeType = _poly.ResolveRequiredType(tp.GetString()!);
                if (!typeToConvert.IsAssignableFrom(runtimeType))
                    throw new JsonException($"Resolved runtime type '{runtimeType}' is not assignable to '{typeToConvert}'.");

                if (!el.TryGetProperty(_poly.ArrayValuePropertyName, out arrayEl) || arrayEl.ValueKind != JsonValueKind.Array)
                    throw new JsonException($"Expected '{_poly.ArrayValuePropertyName}' array payload for '{typeToConvert}'.");
            }

            if (arrayEl.ValueKind != JsonValueKind.Array)
                throw new JsonException($"Expected JSON array for '{typeToConvert}'.");

            // Materialize into a List<TElement?> first (works for arrays and most list types via STJ later).
            var tmp = new List<TElement?>();

            foreach (var item in arrayEl.EnumerateArray())
            {
                var obj = PolymorphyCodec.Read(item, typeof(TElement), options, _poly);
                tmp.Add((TElement?)obj);
            }

            // Convert to the requested list type using STJ (so it can handle arrays, custom list converters, etc.).
            // This is cheap compared to fighting all possible list shapes manually.
            var tmpEl = JsonSerializer.SerializeToElement(tmp, typeof(List<TElement?>), options);
            return (TList?)JsonSerializer.Deserialize(tmpEl, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, TList value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // List slot itself is treated as complex and thus wrapped (NSJ-ish).
            // Elements are written recursively by the codec (objects wrapped, scalars raw).
            PolymorphyCodec.Write(writer, value, options, _poly);
        }
    }
}
