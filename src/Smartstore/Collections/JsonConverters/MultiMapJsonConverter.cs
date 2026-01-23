#nullable enable

using System.Text.Json;
using Smartstore.Json.Polymorphy;
using System.Text.Json.Serialization;

namespace Smartstore.Collections.JsonConverters;

internal class MultiMapJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(Multimap<,>) ||
               genericType == typeof(ConcurrentMultimap<,>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var keyType = typeToConvert.GetGenericArguments()[0];
        var valueType = typeToConvert.GetGenericArguments()[1];

        var genericType = typeToConvert.GetGenericTypeDefinition();

        Type converterType;
        if (genericType == typeof(ConcurrentMultimap<,>))
        {
            converterType = typeof(ConcurrentMultimapConverter<,>)
                .MakeGenericType(keyType, valueType);
        }
        else
        {
            converterType = typeof(MultimapConverter<,>)
                .MakeGenericType(keyType, valueType);
        }

        var isPolymorphicValueType = PolymorphyCodec.IsPolymorphicType(valueType, options);

        return (JsonConverter)Activator.CreateInstance(converterType, [isPolymorphicValueType])!;
    }
}

internal class MultimapConverter<TKey, TValue> : JsonConverter<Multimap<TKey, TValue>>
{
    private readonly bool _isPolymorphicValueType;

    public MultimapConverter(bool isPolymorphicValueType)
    {
        _isPolymorphicValueType = isPolymorphicValueType;
    }

    public override Multimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new Multimap<TKey, TValue>([]);

        var items = DeserializeKeyValuePairs(ref reader, options, _isPolymorphicValueType);

        return typeof(TKey) == typeof(string)
            ? new Multimap<TKey, TValue>(items, (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase)
            : new Multimap<TKey, TValue>(items);
    }

    internal static List<KeyValuePair<TKey, IEnumerable<TValue>>> DeserializeKeyValuePairs(
        ref Utf8JsonReader reader, 
        JsonSerializerOptions options,
        bool isPolymorphicValueType)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected JSON array.");

        var items = new List<KeyValuePair<TKey, IEnumerable<TValue>>>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected JSON object inside array.");

            TKey? key = default;
            IEnumerable<TValue>? value = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected JSON property name.");

                var propertyName = reader.GetString() ?? string.Empty;
                reader.Read();

                if (string.Equals(propertyName, "Key", StringComparison.OrdinalIgnoreCase))
                {
                    key = JsonSerializer.Deserialize<TKey>(ref reader, options);
                }
                else if (string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase))
                {
                    if (!isPolymorphicValueType)
                    {
                        value = JsonSerializer.Deserialize<List<TValue>>(ref reader, options) ?? [];
                    }
                    else
                    {
                        // Uses our stub/envelope pipeline, honors $type within list items.
                        //var tmp = options.ReadPolymorphicList<TValue>(ref reader) ?? [];
                        var tmp = options.DeserializePolymorphic<ICollection<TValue?>>(ref reader) ?? [];

                        // TValue is a non-nullable value type -> List<TValue?> is List<Nullable<TValue>> at runtime.
                        // We must materialize to List<TValue> and reject null items.
                        if (typeof(TValue).IsValueType && Nullable.GetUnderlyingType(typeof(TValue)) is null)
                        {
                            var strong = new List<TValue>(tmp.Count);
                            foreach (var x in tmp)
                            {
                                if (x is null)
                                    throw new JsonException($"Null list item is not allowed for '{typeof(TValue)}'.");

                                strong.Add(x);
                            }
                            value = strong;
                        }
                        else
                        {
                            // Reference types and Nullable<T> can be reinterpreted safely through object.
                            value = (IEnumerable<TValue>)(object)tmp;
                        }
                    }
                }
                else
                {
                    // Skip unknown properties.
                    reader.Skip();
                }
            }

            if (key is null)
                throw new JsonException("Missing required property 'Key'.");

            value ??= [];

            items.Add(new KeyValuePair<TKey, IEnumerable<TValue>>(key, value));
        }

        return items;
    }

    public override void Write(
        Utf8JsonWriter writer, 
        Multimap<TKey, TValue> value, 
        JsonSerializerOptions options)
    {
        WriteCore(writer, value, options, _isPolymorphicValueType);
    }

    internal static void WriteCore(
        Utf8JsonWriter writer,
        IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> value, 
        JsonSerializerOptions options,
        bool isPolymorphicValueType)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            JsonSerializer.Serialize(writer, item.Key, options);

            writer.WritePropertyName("Value");
            if (isPolymorphicValueType)
            {
                options.SerializePolymorphic(writer, item.Value, wrapArrays: true);
            }
            else
            {
                JsonSerializer.Serialize(writer, item.Value, options);
            }

            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

internal class ConcurrentMultimapConverter<TKey, TValue> : JsonConverter<ConcurrentMultimap<TKey, TValue>>
{
    private readonly bool _isPolymorphicValueType;

    public ConcurrentMultimapConverter(bool isPolymorphicValueType)
    {
        _isPolymorphicValueType = isPolymorphicValueType;
    }

    public override ConcurrentMultimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return new ConcurrentMultimap<TKey, TValue>([]);

        var items = MultimapConverter<TKey, TValue>.DeserializeKeyValuePairs(ref reader, options, _isPolymorphicValueType);

        return typeof(TKey) == typeof(string)
            ? new ConcurrentMultimap<TKey, TValue>(items, (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase)
            : new ConcurrentMultimap<TKey, TValue>(items);
    }

    public override void Write(Utf8JsonWriter writer, ConcurrentMultimap<TKey, TValue> value, JsonSerializerOptions options)
    {
        var castedValue = value.Select(kvp => new KeyValuePair<TKey, ICollection<TValue>>(kvp.Key, kvp.Value));
        MultimapConverter<TKey, TValue>.WriteCore(writer, castedValue, options, _isPolymorphicValueType);
    }
}