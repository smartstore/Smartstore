#nullable enable

using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json;

/// <summary>
/// Converter factory for polymorphic "slots": object, interfaces, and abstract base types.
/// Writes {"$type": "...", ...} for object payloads and {"$type":"...","$value":...} for arrays/scalars.
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

    private sealed class PolymorphicObjectConverter<TBase> : JsonConverter<TBase?>
    {
        private readonly PolymorphyOptions _options;

        public PolymorphicObjectConverter(PolymorphyOptions options) => _options = options;

        public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var doc = JsonDocument.ParseValue(ref reader);
            var el = doc.RootElement;

            // Typed wrapper: { "$type": "...", ... } or { "$type": "...", "$value": ... }
            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty(_options.TypePropertyName, out var tp) &&
                tp.ValueKind == JsonValueKind.String)
            {
                var runtimeType = _options.ResolveRequiredType(tp.GetString()!);

                if (!typeof(TBase).IsAssignableFrom(runtimeType))
                {
                    throw new JsonException(
                        $"Resolved runtime type '{runtimeType}' is not assignable to '{typeof(TBase)}'.");
                }

                // $value wrapper for non-object payloads (arrays/scalars)
                if (el.TryGetProperty(_options.ValuePropertyName, out var vp))
                {
                    var obj = JsonSerializer.Deserialize(vp.GetRawText(), runtimeType, options);
                    return (TBase?)obj;
                }

                // Object wrapper: strip $type before deserializing
                var obj2 = DeserializeObjectWithoutType(el, runtimeType, options, _options.TypePropertyName);
                return (TBase?)obj2;
            }

            // Untyped fallback:
            // - only for object slots, to mimic NSJ-ish behavior for missing discriminator cases
            if (typeof(TBase) == typeof(object))
            {
                object? untyped = ReadUntyped(el);
                return (TBase?)untyped;
            }

            throw new JsonException(
                $"Missing '{_options.TypePropertyName}' discriminator for polymorphic slot of type '{typeof(TBase)}'.");
        }

        public override void Write(Utf8JsonWriter writer, TBase? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            var runtimeType = value.GetType();
            var typeId = _options.GetRequiredTypeId(runtimeType);

            // Serialize runtime payload using its own contract (this converter is attached to the base slot, not the runtime type).
            JsonElement payload = JsonSerializer.SerializeToElement(value, runtimeType, options);

            writer.WriteStartObject();
            writer.WriteString(_options.TypePropertyName, typeId); // must be first

            if (payload.ValueKind == JsonValueKind.Object)
            {
                // Copy properties, skipping an existing discriminator if present.
                foreach (var p in payload.EnumerateObject())
                {
                    if (p.NameEquals(_options.TypePropertyName))
                        continue;

                    p.WriteTo(writer);
                }
            }
            else
            {
                // Wrap arrays/scalars so we can keep $type.
                writer.WritePropertyName(_options.ValuePropertyName);
                payload.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        private static object DeserializeObjectWithoutType(JsonElement el, Type runtimeType, JsonSerializerOptions options, string typePropName)
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            using (var w = new Utf8JsonWriter(buffer))
            {
                w.WriteStartObject();

                foreach (var p in el.EnumerateObject())
                {
                    if (p.NameEquals(typePropName))
                        continue;

                    p.WriteTo(w);
                }

                w.WriteEndObject();
            }

            return JsonSerializer.Deserialize(buffer.WrittenSpan, runtimeType, options)
                   ?? throw new JsonException($"Deserialization returned null for '{runtimeType}'.");
        }

        private static object? ReadUntyped(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.String:
                    return el.GetString();

                case JsonValueKind.Number:
                    if (el.TryGetInt64(out var l)) return l;
                    if (el.TryGetDecimal(out var d)) return d;
                    return el.GetDouble();

                case JsonValueKind.Array:
                {
                    var list = new List<object?>();
                    foreach (var item in el.EnumerateArray())
                        list.Add(ReadUntyped(item));
                    return list;
                }

                case JsonValueKind.Object:
                {
                    var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var p in el.EnumerateObject())
                        dict[p.Name] = ReadUntyped(p.Value);
                    return dict;
                }

                default:
                    throw new JsonException($"Unsupported JsonValueKind: {el.ValueKind}");
            }
        }
    }
}