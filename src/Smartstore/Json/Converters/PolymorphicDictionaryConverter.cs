#nullable enable

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.Json.Converters;

/// <summary>
/// Polymorphic dictionary converter factory for "dictionary slots".
/// Supports IDictionary<string, TValue>, Dictionary<string, TValue>, Custom derived dictionaries,
/// and IReadOnlyDictionary<string, TValue> (materializes as Dictionary<string, TValue> on read).
/// </summary>
internal sealed class PolymorphicDictionaryConverterFactory : JsonConverterFactory
{
    private readonly PolymorphyOptions _options;

    public PolymorphicDictionaryConverterFactory(PolymorphyOptions options)
        => _options = Guard.NotNull(options);

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsDictionaryType();
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var isGenericType = typeToConvert.IsDictionaryType(out var keyType, out var valueType);

        // Non-generic IDictionary: handle as string->object dictionary
        if (!isGenericType)
        {
            return new NonGenericStringKeyDictionaryConverter(_options, typeToConvert);
        }

        if (keyType != typeof(string))
        {
            throw new NotSupportedException(
                $"Only string keys are supported (JSON object property names). " +
                $"Type '{typeToConvert}' uses key type '{keyType}'.");
        }

        var convType = typeof(GenericDictionaryConverter<,>).MakeGenericType(typeToConvert, valueType!);
        return (JsonConverter)Activator.CreateInstance(convType, _options)!;
    }

    private sealed class GenericDictionaryConverter<TDict, TValue> : JsonConverter<TDict>
    {
        private readonly PolymorphyOptions _options;

        public GenericDictionaryConverter(PolymorphyOptions options) 
            => _options = options;

        public override TDict? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected JSON object for '{typeToConvert}'.");

            // Create instance:
            // - If property type is interface/abstract (e.g. IReadOnlyDictionary<,>), materialize Dictionary<string, TValue?>.
            // - If it's a concrete derived dictionary type, try to instantiate it.
            object instance = CreateInstance(typeToConvert);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return (TDict)instance;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name.");

                var key = reader.GetString() ?? string.Empty;

                reader.Read(); // move to value token

                // Buffer the value so we can inspect $type anywhere within nested objects.
                using var doc = JsonDocument.ParseValue(ref reader);
                var el = doc.RootElement;

                object? value = PolymorphyCodec.Read(el, typeof(TValue), options, _options);

                AddEntry(instance, key, value);
            }

            throw new JsonException("Incomplete JSON object.");
        }

        public override void Write(Utf8JsonWriter writer, TDict value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (var (k, v) in EnumeratePairs(value))
            {
                writer.WritePropertyName(k);

                // Delegate polymorphic value handling (including legacy $type reads elsewhere) to the shared codec.
                PolymorphyCodec.Write(writer, v, typeof(TValue), options, _options);
            }

            writer.WriteEndObject();
        }

        private static IEnumerable<(string Key, object? Value)> EnumeratePairs(TDict dict)
        {
            // Covers Dictionary<string,TValue>, Custom derived dictionaries, and most interface-based dictionaries.
            if (dict is IEnumerable<KeyValuePair<string, TValue>> strong)
            {
                foreach (var kv in strong)
                    yield return (kv.Key, kv.Value);
                yield break;
            }

            // Covers IDictionary (non-generic) in case a concrete type implements only that.
            if (dict is IDictionary nongeneric)
            {
                foreach (DictionaryEntry de in nongeneric)
                    yield return (Convert.ToString(de.Key) ?? string.Empty, de.Value);
                yield break;
            }

            throw new NotSupportedException(
                $"Type '{typeof(TDict)}' is not enumerable as KeyValuePair<string, {typeof(TValue)}>, nor as IDictionary.");
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
        private readonly PolymorphyOptions _options;
        private readonly Type _targetType;

        public NonGenericStringKeyDictionaryConverter(PolymorphyOptions options, Type targetType)
        {
            _options = options;
            _targetType = targetType;
        }

        public override bool CanConvert(Type typeToConvert) 
            => typeToConvert == _targetType;

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected JSON object for '{typeToConvert}'.");

            object instance;
            try
            {
                instance = Activator.CreateInstance(typeToConvert) ?? new Dictionary<string, object?>(StringComparer.Ordinal);
            }
            catch
            {
                instance = new Dictionary<string, object?>(StringComparer.Ordinal);
            }

            var dict = instance as IDictionary ?? (IDictionary)new Dictionary<string, object?>(StringComparer.Ordinal);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return instance;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected property name.");

                var key = reader.GetString() ?? string.Empty;
                reader.Read();

                using var doc = JsonDocument.ParseValue(ref reader);
                var el = doc.RootElement;

                dict[key] = PolymorphyCodec.Read(el, typeof(object), options, _options);
            }

            throw new JsonException("Incomplete JSON object.");
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

            foreach (DictionaryEntry de in dict)
            {
                writer.WritePropertyName(Convert.ToString(de.Key) ?? string.Empty);
                PolymorphyCodec.Write(writer, de.Value, typeof(object), options, _options);
            }

            writer.WriteEndObject();
        }
    }
}