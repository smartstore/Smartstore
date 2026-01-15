#nullable enable

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Polymorphy;

/// <summary>
/// Polymorphic dictionary converter factory for "dictionary slots".
/// Supports IDictionary<string, TValue>, Dictionary<string, TValue>, Custom derived dictionaries,
/// and IReadOnlyDictionary<string, TValue> (materializes as Dictionary<string, TValue> on read).
/// </summary>
internal sealed class PolymorphicDictionaryConverterFactory : JsonConverterFactory
{
    private readonly PolymorphyOptions _poly;

    public PolymorphicDictionaryConverterFactory(PolymorphyOptions options)
        => _poly = Guard.NotNull(options);

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsDictionaryType();

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var isGenericType = typeToConvert.IsDictionaryType(out var keyType, out var valueType);

        // Non-generic IDictionary: handle as string->object dictionary
        if (!isGenericType)
        {
            return new NonGenericStringKeyDictionaryConverter(_poly, typeToConvert);
        }

        if (keyType != typeof(string))
        {
            throw new NotSupportedException(
                $"Only string keys are supported (JSON object property names). " +
                $"Type '{typeToConvert}' uses key type '{keyType}'.");
        }

        var convType = typeof(GenericDictionaryConverter<,>).MakeGenericType(typeToConvert, valueType!);
        return (JsonConverter)Activator.CreateInstance(convType, _poly)!;
    }

    private sealed class GenericDictionaryConverter<TDict, TValue> : JsonConverter<TDict>
    {
        private readonly PolymorphyOptions _poly;

        public GenericDictionaryConverter(PolymorphyOptions options) 
            => _poly = options;

        public override TDict? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            using var doc = JsonDocument.ParseValue(ref reader);
            var el = doc.RootElement;

            // Accept both:
            // - wrapped dict: {"$type":"...","k":...}
            // - raw object: {"k":...}  (treated as declared type)
            Type runtimeType = typeToConvert;

            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty(_poly.TypePropertyName, out var tp) &&
                tp.ValueKind == JsonValueKind.String)
            {
                var resolved = _poly.ResolveRequiredType(tp.GetString()!);
                if (!typeToConvert.IsAssignableFrom(resolved))
                    throw new JsonException($"Resolved runtime type '{resolved}' is not assignable to '{typeToConvert}'.");

                runtimeType = resolved;
            }
            else if (el.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException($"Expected JSON object for '{typeToConvert}'.");
            }

            object instance = CreateInstance(runtimeType);

            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(_poly.TypePropertyName))
                    continue;

                var value = PolymorphyCodec.ReadValue(p.Value, typeof(TValue), options, _poly);
                AddEntry(instance, p.Name, value);
            }

            return (TDict)instance;
        }

        public override void Write(Utf8JsonWriter writer, TDict value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            // Dictionary slot itself is always wrapped (NSJ-ish).
            writer.WriteStartObject();
            writer.WriteString(_poly.TypePropertyName, _poly.GetRequiredTypeId(value.GetType()));

            foreach (var (k, v) in EnumeratePairs(value))
            {
                writer.WritePropertyName(k);

                // Values flow through codec so nested dicts/arrays/objects get wrapped recursively.
                // Arrays in dict-values use PolymorphyOptions.WrapDictionaryArrays.
                PolymorphyCodec.WriteObjectSlot(writer, v, options, _poly);
            }

            writer.WriteEndObject();
        }

        private static IEnumerable<(string Key, object? Value)> EnumeratePairs(TDict dict)
        {
            if (dict is IEnumerable<KeyValuePair<string, TValue>> strong)
            {
                foreach (var kv in strong)
                    yield return (kv.Key, kv.Value);
                yield break;
            }

            if (dict is IDictionary nongeneric)
            {
                foreach (DictionaryEntry de in nongeneric)
                    yield return (Convert.ToString(de.Key) ?? string.Empty, de.Value);
                yield break;
            }

            throw new NotSupportedException($"Type '{typeof(TDict)}' is not enumerable as KeyValuePair<string, {typeof(TValue)}>, nor as IDictionary.");
        }

        private static object CreateInstance(Type targetType)
        {
            // Interface/abstract (IDictionary<,> / IReadOnlyDictionary<,>) => materialize Dictionary<string, TValue?>
            if (targetType.IsInterface || targetType.IsAbstract)
                return new Dictionary<string, TValue?>(StringComparer.Ordinal);

            // Try to instantiate the concrete type (keeps derived dictionaries like CustomPropertiesDictionary)
            try
            {
                var obj = Activator.CreateInstance(targetType);
                if (obj is not null)
                    return obj;
            }
            catch
            {
                // Fall back below
            }

            return new Dictionary<string, TValue?>(StringComparer.Ordinal);
        }

        private static void AddEntry(object dictInstance, string key, object? value)
        {
            // Preferred path: IDictionary<string, TValue?>
            if (dictInstance is IDictionary<string, TValue?> strong)
            {
                strong[key] = (TValue?)value;
                return;
            }

            // Derived dictionaries often implement IDictionary<string, object> or only IDictionary
            if (dictInstance is IDictionary nongeneric)
            {
                nongeneric[key] = value;
                return;
            }

            // Fallback: ICollection<KeyValuePair<string, TValue?>>
            if (dictInstance is ICollection<KeyValuePair<string, TValue?>> coll)
            {
                coll.Add(new KeyValuePair<string, TValue?>(key, (TValue?)value));
                return;
            }

            throw new NotSupportedException(
                $"Cannot add entries to dictionary instance of type '{dictInstance.GetType()}'.");
        }
    }

    private sealed class NonGenericStringKeyDictionaryConverter : JsonConverter<object>
    {
        private readonly PolymorphyOptions _poly;
        private readonly Type _targetType;

        public NonGenericStringKeyDictionaryConverter(PolymorphyOptions options, Type targetType)
        {
            _poly = options;
            _targetType = targetType;
        }

        public override bool CanConvert(Type typeToConvert) 
            => typeToConvert == _targetType;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var el = doc.RootElement;

            if (el.ValueKind != JsonValueKind.Object)
                throw new JsonException($"Expected JSON object for '{typeToConvert}'.");

            // Same rule: accept wrapped or raw object.
            Type runtimeType = typeToConvert;

            if (el.TryGetProperty(_poly.TypePropertyName, out var tp) && tp.ValueKind == JsonValueKind.String)
            {
                var resolved = _poly.ResolveRequiredType(tp.GetString()!);
                if (!typeToConvert.IsAssignableFrom(resolved))
                    throw new JsonException($"Resolved runtime type '{resolved}' is not assignable to '{typeToConvert}'.");

                runtimeType = resolved;
            }

            object instance;
            try
            {
                instance = Activator.CreateInstance(runtimeType) ?? new Dictionary<string, object?>(StringComparer.Ordinal);
            }
            catch
            {
                instance = new Dictionary<string, object?>(StringComparer.Ordinal);
            }

            var dict = instance as IDictionary ?? (IDictionary)new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var p in el.EnumerateObject())
            {
                if (p.NameEquals(_poly.TypePropertyName))
                    continue;

                dict[p.Name] = PolymorphyCodec.Read(p.Value, typeof(object), options, _poly);
            }

            return instance;
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value is not IDictionary dict)
                throw new JsonException($"Expected IDictionary, got '{value.GetType()}'.");

            writer.WriteStartObject();
            writer.WriteString(_poly.TypePropertyName, _poly.GetRequiredTypeId(value.GetType()));

            foreach (DictionaryEntry de in dict)
            {
                writer.WritePropertyName(Convert.ToString(de.Key) ?? string.Empty);
                PolymorphyCodec.WriteObjectSlot(writer, de.Value, options, _poly);
            }

            writer.WriteEndObject();
        }
    }
}