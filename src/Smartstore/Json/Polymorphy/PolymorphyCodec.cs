#nullable enable

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Json.Polymorphy;

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

            // Wrapped array payload: {"$type":"...","$values":[...]}
            if (el.TryGetProperty(o.ArrayValuePropertyName, out var valuesEl))
            {
                return JsonSerializer.Deserialize(valuesEl, runtimeType, readOptions);
            }

            // Object payload: strip discriminator at this level; nested $type remain.
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
                // Nested wrapper support.
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

    /// <summary>
    /// Writes a polymorphic value in a "NSJ-ish" form.
    /// Slot rules:
    /// - WrapArrays == false: arrays/lists are always written raw ([...]) everywhere.
    /// - Object slot: if the object itself is an array/list and WrapArrays == true => wrap ONLY this root array/list.
    /// - List slot: if WrapArrays == true => wrap the list AND any nested lists recursively.
    /// - Dictionary slot: dictionary itself is always wrapped as an object; if WrapArrays == true => nested lists under its values are wrapped recursively.
    /// </summary>
    internal static void WriteObjectSlot(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        WriteCore(writer, value, options, o, PolymorphyKind.ObjectSlot, wrapArraysScope: false);
    }

    internal static void WriteListSlot(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        // List slot enables recursive wrapping of nested lists when WrapArrays is enabled.
        WriteCore(writer, value, options, o, PolymorphyKind.ListSlot, wrapArraysScope: o.WrapArrays);
    }

    internal static void WriteDictionarySlot(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        // Dictionary slot enables recursive wrapping of nested lists under its values when WrapArrays is enabled.
        WriteCore(writer, value, options, o, PolymorphyKind.DictionarySlot, wrapArraysScope: o.WrapArrays);
    }

    /// <summary>
    /// Backward-compatible entry point. Treat as "object slot".
    /// </summary>
    public static void Write(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        WriteObjectSlot(writer, value, options, o);
    }

    private static void WriteCore(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o,
        PolymorphyKind slotKind,
        bool wrapArraysScope)
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

        // Dictionaries are treated as complex objects and always wrapped at the dictionary slot root.
        if (TryGetStringKeyDictionary(value, out var dict))
        {
            WriteWrappedObjectStart(writer, runtimeType, o);

            foreach (var (key, val) in dict)
            {
                writer.WritePropertyName(key);

                // Dictionary value recursion:
                // - Keep wrapArraysScope (enables nested list wrapping when WrapArrays==true).
                // - Values behave like object-slot items (we don't want "list slot" behavior for every value).
                WriteCore(writer, val, options, o, PolymorphyKind.ObjectSlot, wrapArraysScope);
            }

            writer.WriteEndObject();
            return;
        }

        // Enumerables (arrays/lists) are treated as arrays.
        if (TryGetEnumerable(value, out var enumerable))
        {
            // Wrap decision:
            // - WrapArrays must be true at all.
            // - Object slot: wrap ONLY the root array/list (do not enable scope for nested lists).
            // - List slot: always wrap root list and keep scope for nested lists.
            // - Dictionary slot: this branch is mostly for dictionary *values* that are lists; scope decides recursion.
            var shouldWrapArray =
                o.WrapArrays &&
                (slotKind == PolymorphyKind.ListSlot
                 || slotKind == PolymorphyKind.ObjectSlot
                 || wrapArraysScope);

            if (shouldWrapArray)
            {
                // Wrapped array: {"$type":"...","$values":[ ... ]}
                WriteWrappedObjectStart(writer, runtimeType, o);
                writer.WritePropertyName(o.ArrayValuePropertyName);

                writer.WriteStartArray();

                foreach (var item in enumerable)
                {
                    // Important:
                    // - Object slot root wrapping must NOT propagate scope.
                    // - List/dict scopes propagate wrapArraysScope so nested lists get wrapped.
                    var nextScope = (slotKind == PolymorphyKind.ObjectSlot) ? false : wrapArraysScope;
                    WriteCore(writer, item, options, o, PolymorphyKind.ObjectSlot, nextScope);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            else
            {
                // Raw array: [ ... ] BUT elements are still recursively written
                writer.WriteStartArray();

                foreach (var item in enumerable)
                {
                    // If scope is enabled (list/dict), nested lists may still be wrapped.
                    WriteCore(writer, item, options, o, PolymorphyKind.ObjectSlot, wrapArraysScope);
                }

                writer.WriteEndArray();
            }

            return;
        }

        // POCO / complex object:
        // Let STJ decide *which* properties to write by serializing to an element first.
        // Then write only those properties, but recurse using CLR values so nested complex objects get wrapped.
        var payload = JsonSerializer.SerializeToElement(value, runtimeType, options);

        if (payload.ValueKind != JsonValueKind.Object)
        {
            // Defensive fallback: should not happen for POCOs, but keep behavior predictable.
            JsonSerializer.Serialize(writer, value, runtimeType, options);
            return;
        }

        WriteWrappedObjectStart(writer, runtimeType, o);

        foreach (var p in payload.EnumerateObject())
        {
            if (p.NameEquals(o.TypePropertyName))
                continue;

            writer.WritePropertyName(p.Name);

            // IMPORTANT:
            // Do NOT recurse into POCO properties here.
            // STJ already serialized those properties correctly (including any custom converters
            // on nested polymorphic slots), and strongly typed properties must stay unwrapped.
            p.Value.WriteTo(writer);
        }

        writer.WriteEndObject();
    }

    private static void WriteWrappedObjectStart(Utf8JsonWriter writer, Type runtimeType, PolymorphyOptions o)
    {
        writer.WriteStartObject();
        writer.WriteString(o.TypePropertyName, o.GetRequiredTypeId(runtimeType));
    }

    internal static bool IsScalarLike(Type t)
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

    #endregion
}
