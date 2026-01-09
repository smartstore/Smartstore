#nullable enable

using System.Buffers;
using System.Text.Json;

namespace Smartstore.Json;

/// <summary>
/// Shared polymorphic read/write logic used by Object/List/Dictionary converters.
/// Keeps legacy-NSJ "Objects" readable by recognizing "$type" at any nested object level.
/// </summary>
internal static class PolymorphyCodec
{
    // Writes {"$type":"..."} + payload properties for objects,
    // and {"$type":"...","$value":...} for non-object payloads.
    public static void Write(
        Utf8JsonWriter writer, 
        object? value, 
        Type declaredBaseType, 
        JsonSerializerOptions options, 
        PolymorphyOptions o)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var runtimeType = value.GetType();
        var typeId = o.GetRequiredTypeId(runtimeType);

        var payload = JsonSerializer.SerializeToElement(value, runtimeType, options);

        writer.WriteStartObject();
        writer.WriteString(o.TypePropertyName, typeId);

        if (payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in payload.EnumerateObject())
            {
                if (p.NameEquals(o.TypePropertyName))
                    continue;

                p.WriteTo(writer);
            }
        }
        else
        {
            writer.WritePropertyName(o.ValuePropertyName);
            payload.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    // Reads:
    // - legacy NSJ Objects: {"$type":"...","Prop":...}
    // - our wrapper for scalars/arrays: {"$type":"...","$value":...}
    // If declaredBaseType == object and no $type, returns untyped tree, but still respects nested $type.
    public static object? Read(
        JsonElement el, 
        Type declaredBaseType, 
        JsonSerializerOptions options, 
        PolymorphyOptions o)
    {
        if (el.ValueKind == JsonValueKind.Null || el.ValueKind == JsonValueKind.Undefined)
            return null;

        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(o.TypePropertyName, out var tp) &&
            tp.ValueKind == JsonValueKind.String)
        {
            var runtimeType = o.ResolveRequiredType(tp.GetString()!);

            if (declaredBaseType != typeof(object) && !declaredBaseType.IsAssignableFrom(runtimeType))
                throw new JsonException($"Resolved runtime type '{runtimeType}' is not assignable to '{declaredBaseType}'.");

            if (el.TryGetProperty(o.ValuePropertyName, out var vp))
                return JsonSerializer.Deserialize(vp.GetRawText(), runtimeType, options);

            var json = SerializeObjectWithoutType(el, o.TypePropertyName);
            return JsonSerializer.Deserialize(json, runtimeType, options);
        }

        if (declaredBaseType == typeof(object))
            return ReadUntyped(el, options, o);

        throw new JsonException($"Missing '{o.TypePropertyName}' discriminator for polymorphic slot '{declaredBaseType}'.");
    }

    private static object? ReadUntyped(JsonElement el, JsonSerializerOptions options, PolymorphyOptions o)
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
                    list.Add(ReadUntyped(item, options, o));
                return list;
            }

            case JsonValueKind.Object:
            {
                // IMPORTANT: nested objects may carry $type (legacy NSJ Objects).
                if (el.TryGetProperty(o.TypePropertyName, out var tp) && tp.ValueKind == JsonValueKind.String)
                    return Read(el, typeof(object), options, o);

                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var p in el.EnumerateObject())
                    dict[p.Name] = ReadUntyped(p.Value, options, o);
                return dict;
            }

            default:
                throw new JsonException($"Unsupported JsonValueKind: {el.ValueKind}");
        }
    }

    private static ReadOnlySpan<byte> SerializeObjectWithoutType(JsonElement el, string typePropName)
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

        return buffer.WrittenSpan;
    }
}
