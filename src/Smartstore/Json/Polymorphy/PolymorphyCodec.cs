#nullable enable

using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Polymorphy;

/// <summary>
/// Shared polymorphic read/write logic used by Object/List/Dictionary converters.
/// Keeps legacy-NSJ "Objects" readable by recognizing "$type" at any nested object level.
/// </summary>
internal static class PolymorphyCodec
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> _lenientOptionsCache = [];

    #region Read

    public static object? ReadValue(
        JsonElement el, 
        Type declaredType, 
        JsonSerializerOptions options, 
        PolymorphyOptions poly)
    {
        return IsPolymorphicType(declaredType)
            ? Read(el, declaredType, options, poly)
            : JsonSerializer.Deserialize(el, declaredType, options);
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
                // If the target is a sequence with polymorphic element type, we must read elements
                // via our codec so nested $type gets honored (otherwise STJ yields JsonNode/JsonElement).
                if (runtimeType.IsSequenceType(out var elementType) &&
                    IsPolymorphicType(elementType) &&
                    valuesEl.ValueKind == JsonValueKind.Array)
                {
                    return ReadPolymorphArray(valuesEl, runtimeType, elementType, readOptions, o);
                }

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
        if (el.TryGetScalarValue(out var value))
        {
            return value;
        }
        
        switch (el.ValueKind)
        {
            case JsonValueKind.Array:
            {
                var list = new List<object?>();
                foreach (var item in el.EnumerateArray())
                    list.Add(Read(item, typeof(object), options, o)); // <- Use $type if available
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

    private static object? ReadPolymorphArray(
        JsonElement arrayEl,
        Type targetSequenceType,
        Type elementType,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        // 1) Build List<TElement> with correctly typed runtime instances (via $type).
        var listType = typeof(List<>).MakeGenericType(elementType);
        var typedList = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in arrayEl.EnumerateArray())
        {
            var elem = Read(item, elementType, options, o);
            typedList.Add(elem);
        }

        // 2) Arrays: create directly.
        if (targetSequenceType.IsArray)
        {
            var arr = Array.CreateInstance(elementType, typedList.Count);
            for (int i = 0; i < typedList.Count; i++)
                arr.SetValue(typedList[i], i);

            return arr;
        }

        // 3) If the requested target type can accept List<T> as-is (e.g. IReadOnlyList<T>, IEnumerable<T>, IList<T>, ICollection<T>).
        if (targetSequenceType.IsAssignableFrom(listType))
            return typedList;

        // 4) Create a suitable collection instance (e.g. HashSet<T> for ISet<T>) and populate it without JSON.
        var instance = CreateCollectionInstance(targetSequenceType, elementType, typedList);

        // Try non-generic IList (rare but cheap)
        if (instance is IList ilist)
        {
            foreach (var it in typedList)
                ilist.Add(it);
            return instance;
        }

        // Try ICollection<T>.Add(T)
        var add = instance.GetType().GetMethod("Add", [elementType]);
        if (add is not null)
        {
            foreach (var it in typedList)
                add.Invoke(instance, [it]);
            return instance;
        }

        // Last resort: if we cannot add, fall back to List<T> for interface/abstract targets.
        if (targetSequenceType.IsInterface || targetSequenceType.IsAbstract)
            return typedList;

        throw new JsonException($"Cannot populate collection type '{targetSequenceType}' for element type '{elementType}'.");
    }

    internal static object CreateAndPopulateCollectionInstance(Type targetType, Type elementType, IList typedList)
    {
        // Arrays: we must allocate and copy explicitly.
        if (targetType.IsArray)
        {
            var arr = Array.CreateInstance(elementType, typedList.Count);
            typedList.CopyTo(arr, 0);
            return arr;
        }

        var instance = CreateCollectionInstance(targetType, elementType, typedList);

        // If the ctor(IEnumerable<T>) path was used, the instance is already populated.
        // Otherwise, we need to add items.
        if (!IsEmptyCollection(instance))
            return instance;

        PopulateCollection(instance, elementType, typedList);
        return instance;
    }

    private static object CreateCollectionInstance(Type targetType, Type elementType, IList typedList)
    {
        // Determine a concrete type to instantiate.
        var concreteType = ResolveConcreteCollectionType(targetType, elementType);

        // Prefer ctor(IEnumerable<T>) so types like HashSet<T> can build efficiently.
        var enumerableOfT = typeof(IEnumerable<>).MakeGenericType(elementType);
        var ctor = concreteType.GetConstructor([enumerableOfT]);
        if (ctor is not null)
        {
            // typedList is actually List<T>, which implements IEnumerable<T>.
            return ctor.Invoke([typedList]);
        }

        // Parameterless ctor.
        try
        {
            var obj = Activator.CreateInstance(concreteType);
            if (obj is not null)
                return obj;
        }
        catch
        {
            // ignore and fall back below
        }

        // Fallback based on "shape".
        var fallback = ResolveConcreteCollectionType(targetType, elementType, forceFallback: true);
        return Activator.CreateInstance(fallback)!;
    }

    private static bool IsEmptyCollection(object instance)
    {
        // Best-effort fast checks. If we can't prove it's populated, we populate.
        return instance switch
        {
            ICollection c => c.Count == 0,
            _ => true
        };
    }

    private static void PopulateCollection(object instance, Type elementType, IList typedList)
    {
        // Non-generic IList
        if (instance is IList nongeneric)
        {
            foreach (var item in typedList)
                nongeneric.Add(item);
            return;
        }

        // ICollection<T>
        var iCollectionOfT = typeof(ICollection<>).MakeGenericType(elementType);
        if (iCollectionOfT.IsInstanceOfType(instance))
        {
            var add = iCollectionOfT.GetMethod("Add", [elementType])!;
            foreach (var item in typedList)
                add.Invoke(instance, [item]);
            return;
        }

        // Last resort: try Add(T) on the concrete type.
        var addMethod = instance.GetType().GetMethod("Add", [elementType]);
        if (addMethod is not null)
        {
            foreach (var item in typedList)
                addMethod.Invoke(instance, [item]);
            return;
        }

        throw new NotSupportedException($"Cannot populate collection type '{instance.GetType()}'.");
    }

    private static Type ResolveConcreteCollectionType(Type targetType, Type elementType, bool forceFallback = false)
    {
        // Interfaces/abstracts (and forced fallback) => choose a sensible concrete type.
        if (forceFallback || targetType.IsInterface || targetType.IsAbstract)
        {
            // Set-like => HashSet<T>
            if (targetType.IsSetType(out var t) && t == elementType)
                return typeof(HashSet<>).MakeGenericType(elementType);

            // Everything else => List<T> (covers IReadOnlyList/IReadOnlyCollection/IEnumerable/ICollection/IList)
            return typeof(List<>).MakeGenericType(elementType);
        }

        // Concrete types:
        // If it's a ReadOnlyCollection<T> or similar without parameterless ctor, we handle via IEnumerable<T> ctor above.
        return targetType;
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
    public static void WriteObjectSlot(
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

        // Root special-case: object-slot containing an array/list.
        if (o.WrapArrays && TryGetEnumerable(value, out _))
        {
            WriteCore(writer, value, options, o, PolymorphyKind.ListSlot, wrapArraysScope: false);
            return;
        }

        WriteCore(writer, value, options, o, PolymorphyKind.ObjectSlot, wrapArraysScope: false);
    }

    public static void WriteListSlot(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options,
        PolymorphyOptions o)
    {
        // List slot enables recursive wrapping of nested lists when WrapArrays is enabled.
        WriteCore(writer, value, options, o, PolymorphyKind.ListSlot, wrapArraysScope: o.WrapArrays);
    }

    public static void WriteDictionarySlot(
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
            // Don't wrap dictionary root if it's Dictionary<string, object?> (the most common polymorphic case).
            writer.WriteStartObject();
            if (dict.GetType() != typeof(Dictionary<string, object?>))
            {
                writer.WriteString(o.TypePropertyName, o.GetRequiredTypeId(runtimeType));
            }

            foreach (var (key, val) in dict)
            {
                writer.WritePropertyName(key);

                // Dictionary values behave like object-slot values, but inherit wrapArraysScope
                // so nested lists can be wrapped when WrapArrays is enabled for the dictionary slot.
                WriteCore(writer, val, options, o, PolymorphyKind.ObjectSlot, wrapArraysScope);
            }

            writer.WriteEndObject();
            return;
        }

        // Enumerables (arrays/lists) are treated as arrays.
        if (TryGetEnumerable(value, out var enumerable))
        {
            var shouldWrapArray =
                o.WrapArrays &&
                (slotKind == PolymorphyKind.ListSlot || wrapArraysScope);

            // Special-case: sequences with polymorphic element types need per-element wrapping,
            // otherwise STJ cannot roundtrip concrete runtime types (e.g. List<object>, List<IBase>, List<IInterface>).
            var isPolymorphElement = runtimeType.IsSequenceType(out var elementType) && IsPolymorphicType(elementType);

            if (isPolymorphElement)
            {
                if (shouldWrapArray)
                {
                    // Wrapped array: {"$type":"...","$values":[...]}
                    WriteWrappedObjectStart(writer, runtimeType, o);
                    writer.WritePropertyName(o.ArrayValuePropertyName);
                }

                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    // Each element behaves like an object-slot.
                    WriteObjectSlot(writer, item, options, o);
                }
                writer.WriteEndArray();

                if (shouldWrapArray)
                    writer.WriteEndObject();

                return;
            }

            // Default path: delegate payload to STJ so modifiers/default ignore apply.
            if (shouldWrapArray)
            {
                WriteWrappedObjectStart(writer, runtimeType, o);
                writer.WritePropertyName(o.ArrayValuePropertyName);

                // Let STJ write the array payload (so nested converters/modifiers kick in).
                JsonSerializer.Serialize(writer, value, runtimeType, options);

                writer.WriteEndObject();
            }
            else
            {
                // Raw array payload written by STJ.
                JsonSerializer.Serialize(writer, value, runtimeType, options);
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

    internal static void WriteWrappedObjectStart(Utf8JsonWriter writer, Type runtimeType, PolymorphyOptions o)
    {
        writer.WriteStartObject();
        writer.WriteString(o.TypePropertyName, o.GetRequiredTypeId(runtimeType));
    }

    public static bool IsScalarLike(Type t)
    {
        return t.IsBasicType()
            || t == typeof(Uri)
            || t == typeof(JsonElement)
            || t == typeof(JsonDocument);
    }

    public static bool IsPolymorphicType(Type t)
    {
        if (t == typeof(object))
            return true;

        // Polymorhic types with a custom converter(e.g.IPermissionNode) can be handled by STJ directly.
        if (t.IsAbstract || t.IsInterface)
            return !t.HasAttribute<JsonConverterAttribute>(false);

        return false;
    }

    public static bool TryGetPolymorphyKind(Type t, [NotNullWhen(true)] out PolymorphyKind? kind, [NotNullWhen(true)] out Type? elementType)
    {
        kind = default;
        elementType = null;

        if (t.IsDictionaryType(out var keyType, out var valueType))
        {
            if (keyType == typeof(string) && IsPolymorphicType(valueType))
            {
                kind = PolymorphyKind.DictionarySlot;
                elementType = valueType;
                return true;
            }
        }
        else if (t.IsSequenceType(out var itemType))
        {
            if (IsPolymorphicType(itemType))
            {
                kind = PolymorphyKind.ListSlot;
                elementType = itemType;
                return true;
            }
                
        }
        else if (IsPolymorphicType(t))
        {
            kind = PolymorphyKind.ObjectSlot;
            elementType = t;
            return true;
        }

        return false;
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
