#nullable enable

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Json;

/// <summary>
/// Shared polymorphic read/write logic used by Object/List/Dictionary converters.
/// Keeps legacy-NSJ "Objects" readable by recognizing "$type" at any nested object level.
/// </summary>
internal static class PolymorphyCodec
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _lenientOptionsCache = [];

    #region Read

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
        if (el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        if (el.ValueKind == JsonValueKind.Object &&
            el.TryGetProperty(o.TypePropertyName, out var tp) &&
            tp.ValueKind == JsonValueKind.String)
        {
            var runtimeType = o.ResolveRequiredType(tp.GetString()!);

            if (declaredBaseType != typeof(object) && !declaredBaseType.IsAssignableFrom(runtimeType))
                throw new JsonException($"Resolved runtime type '{runtimeType}' is not assignable to '{declaredBaseType}'.");

            var readOptions = GetEffectiveReadOptions(options);

            // Wrapped scalar/array payloads: {"$type":"...","$value":...} / {"$type":"...","$values":[...]}
            if (TryGetWrappedPayload(el, o, out var payload))
            {
                // Deserialize directly from JsonElement to avoid GetRawText() allocations.
                return JsonSerializer.Deserialize(payload, runtimeType, readOptions);
            }

            // Object payload: remove only the discriminator at this level.
            // Nested $type are intentionally kept; lenient options ensure typed POCOs ignore them.
            var jsonBytes = SerializeObjectWithoutType(el, o.TypePropertyName);
            return JsonSerializer.Deserialize(jsonBytes, runtimeType, readOptions);
        }

        if (declaredBaseType == typeof(object))
            return ReadUntyped(el, options, o);

        throw new JsonException($"Missing '{o.TypePropertyName}' discriminator for polymorphic slot '{declaredBaseType}'.");
    }

    private static JsonSerializerOptions GetEffectiveReadOptions(JsonSerializerOptions options)
    {
        // Default is Skip anyway -> no clone, no cache indirection needed.
        if (options.UnmappedMemberHandling != JsonUnmappedMemberHandling.Disallow)
            return options;

        return _lenientOptionsCache.GetValue(options, static o =>
        {
            var clone = new JsonSerializerOptions(o)
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
            };

            return clone;
        });
    }

    private static bool TryGetWrappedPayload(JsonElement wrapper, PolymorphyOptions o, out JsonElement payload)
    {
        // Array wrapper first (NSJ uses $values for arrays)
        if (wrapper.TryGetProperty(o.ArrayValuePropertyName, out payload))
            return true;

        // Scalar wrapper ($value)
        if (wrapper.TryGetProperty(o.ScalarValuePropertyName, out payload))
            return true;

        payload = default;
        return false;
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
                if (el.TryGetInt64(out var l))
                    return l;

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
                // Critical: any nested object may be a polymorphic wrapper (including ones you now write everywhere).
                if (el.TryGetProperty(o.TypePropertyName, out var tp) && tp.ValueKind == JsonValueKind.String)
                    return Read(el, typeof(object), options, o);

                // Plain JSON object -> untyped dictionary
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var p in el.EnumerateObject())
                    dict[p.Name] = ReadUntyped(p.Value, options, o);
                return dict;
            }

            default:
                throw new JsonException($"Unsupported JsonValueKind: {el.ValueKind}");
        }
    }

    private static byte[] SerializeObjectWithoutType(JsonElement el, string typePropName)
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

        // Return a real array so the data remains rooted (safe for GC).
        return buffer.WrittenSpan.ToArray();
    }

    #endregion

    #region Write

    public static void Write(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        WriteCore(writer, value, options, o);
    }

    // Writes {"$type":"..."} + payload properties for objects,
    // and {"$type":"...","$value":...} for non-object payloads.
    private static void WriteCore(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var runtimeType = value.GetType();

        // Scalars are written raw (NSJ-ish)
        if (IsScalarLike(runtimeType))
        {
            JsonSerializer.Serialize(writer, value, runtimeType, options);
            return;
        }

        // Dictionaries are treated as complex objects and always wrapped
        if (TryGetStringKeyDictionary(value, out var dict))
        {
            WriteWrappedObjectStart(writer, runtimeType, o);

            foreach (var (key, val) in dict)
            {
                writer.WritePropertyName(key);
                WriteCore(writer, val, options, o);
            }

            writer.WriteEndObject();
            return;
        }

        // Enumerables (arrays/lists) are treated as arrays
        if (TryGetEnumerable(value, out var enumerable))
        {
            if (o.WrapDictionaryArrays)
            {
                // Wrapped array: {"$type":"...","$values":[ ... ]}
                writer.WriteStartObject();
                writer.WriteString(o.TypePropertyName, o.GetRequiredTypeId(runtimeType));
                writer.WritePropertyName(o.ArrayValuePropertyName);

                writer.WriteStartArray();
                foreach (var item in enumerable)
                    WriteCore(writer, item, options, o);
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            else
            {
                // Raw array: [ ... ] BUT elements are still recursively written
                writer.WriteStartArray();
                foreach (var item in enumerable)
                    WriteCore(writer, item, options, o);
                writer.WriteEndArray();
            }

            return;
        }

        // POCO / complex object: always wrapped and properties recursively written
        WriteWrappedObjectStart(writer, runtimeType, o);

        foreach (var (jsonName, propValue) in EnumerateSerializableMembers(value, runtimeType, options, o))
        {
            writer.WritePropertyName(jsonName);
            WriteCore(writer, propValue, options, o);
        }

        writer.WriteEndObject();
    }

    private static void WriteWrappedObjectStart(Utf8JsonWriter writer, Type runtimeType, PolymorphyOptions o)
    {
        writer.WriteStartObject();
        writer.WriteString(o.TypePropertyName, o.GetRequiredTypeId(runtimeType));
    }

    private static bool IsScalarLike(Type t)
    {
        return t.IsBasicType()
            || t == typeof(Uri)
            || t == typeof(JsonElement)
            || t == typeof(JsonDocument);
    }

    private static bool TryGetEnumerable(object value, out IEnumerable enumerable)
    {
        // string is IEnumerable<char>, but should be scalar
        if (value is string)
        {
            enumerable = null!;
            return false;
        }

        // IDictionary is also IEnumerable, but handled separately
        if (value is IDictionary)
        {
            enumerable = null!;
            return false;
        }

        if (value is IEnumerable e)
        {
            enumerable = e;
            return true;
        }

        enumerable = null!;
        return false;
    }

    private static bool TryGetStringKeyDictionary(object value, out IEnumerable<(string Key, object? Value)> dict)
    {
        if (value is IDictionary nongeneric)
        {
            dict = EnumerateNonGenericDictionary(nongeneric);
            return true;
        }

        var t = value.GetType();

        if (t.IsDictionaryType(out _, out var valueType))
        {
            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType);
            dict = EnumerateKeyValuePairs(value, kvpType);
            return true;
        }

        dict = null!;
        return false;
    }

    private static IEnumerable<(string Key, object? Value)> EnumerateNonGenericDictionary(IDictionary dict)
    {
        foreach (DictionaryEntry de in dict)
            yield return (Convert.ToString(de.Key) ?? string.Empty, de.Value);
    }

    private static IEnumerable<(string Key, object? Value)> EnumerateKeyValuePairs(object value, Type kvpType)
    {
        // kvpType is KeyValuePair<string, TValue>
        var keyProp = kvpType.GetProperty("Key")!;
        var valProp = kvpType.GetProperty("Value")!;

        foreach (var item in (IEnumerable)value)
        {
            if (item is null)
                continue;

            var k = (string?)keyProp.GetValue(item) ?? string.Empty;
            var v = valProp.GetValue(item);
            yield return (k, v);
        }
    }

    private static IEnumerable<(string JsonName, object? Value)> EnumerateSerializableMembers(
        object instance,
        Type runtimeType,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        var ti = options.GetTypeInfo(runtimeType);

        if (ti.Kind != JsonTypeInfoKind.Object)
            yield break;

        foreach (var p in ti.Properties)
        {
            if (p.IsExtensionData)
                continue;

            // Avoid collisions with our discriminator
            if (string.Equals(p.Name, o.TypePropertyName, StringComparison.Ordinal))
                continue;

            // If Get is null, STJ treats it as "skip on serialization"
            var getter = p.Get;
            if (getter is null)
                continue;

            var value = getter(instance);

            if (value is null && options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                continue;

            var shouldSerialize = p.ShouldSerialize;
            if (shouldSerialize != null && !shouldSerialize(instance, value)) continue;

            yield return (p.Name, value);
        }
    }

    #endregion
}
