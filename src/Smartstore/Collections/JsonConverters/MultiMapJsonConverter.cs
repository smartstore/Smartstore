using System.Collections;
using System.Text.Json;
using NSJ = Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;

namespace Smartstore.Collections.JsonConverters;

#region NSJ

internal class MultiMapJsonConverter : NSJ.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Multimap<,>);
        return canConvert;
    }

    public override object ReadJson(NSJ.JsonReader reader, Type objectType, object existingValue, NSJ.JsonSerializer serializer)
    {
        // typeof TKey
        var keyType = objectType.GetGenericArguments()[0];

        // typeof TValue
        var valueType = objectType.GetGenericArguments()[1];

        // typeof IEnumerable<KeyValuePair<TKey, ICollection<TValue>>
        var sequenceType = typeof(IEnumerable<>)
            .MakeGenericType(typeof(KeyValuePair<,>)
            .MakeGenericType(keyType, typeof(IEnumerable<>)
            .MakeGenericType(valueType)));

        // serialize JArray to sequenceType
        var list = serializer.Deserialize(reader, sequenceType);

        if (keyType == typeof(string))
        {
            // call constructor Multimap(IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> items, IEqualityComparer<TKey> comparer)
            // TBD: we always assume string keys to be case insensitive. Serialize it somehow and fetch here!
            return Activator.CreateInstance(objectType, new object[] { list, StringComparer.OrdinalIgnoreCase });
        }
        else
        {
            // call constructor Multimap(IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> items)
            return Activator.CreateInstance(objectType, new object[] { list });
        }
    }

    public override void WriteJson(NSJ.JsonWriter writer, object value, NSJ.JsonSerializer serializer)
    {
        writer.WriteStartArray();
        {
            var enumerable = value as IEnumerable;
            foreach (var item in enumerable)
            {
                // Json.Net uses a converter for KeyValuePair here
                serializer.Serialize(writer, item);
            }
        }
        writer.WriteEndArray();
    }
}

internal sealed class ConcurrentMultiMapConverter : MultiMapJsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ConcurrentMultimap<,>);
        return canConvert;
    }
}

#endregion

#region STJ

internal class MultiMapConverterFactory : STJ.JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(Multimap<,>) ||
               genericType == typeof(ConcurrentMultimap<,>);
    }

    public override STJ.JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
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

        return (STJ.JsonConverter)Activator.CreateInstance(converterType);
    }
}

internal class MultimapConverter<TKey, TValue> : STJ.JsonConverter<Multimap<TKey, TValue>>
{
    public override Multimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TODO: (json) (mc) Polymorphy?
        var list = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>>(ref reader, options);

        return typeof(TKey) == typeof(string)
            ? new Multimap<TKey, TValue>(list, (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase)
            : new Multimap<TKey, TValue>(list);
    }

    public override void Write(Utf8JsonWriter writer, Multimap<TKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, item.GetType(), options);
        }
        writer.WriteEndArray();
    }
}

internal class ConcurrentMultimapConverter<TKey, TValue> : STJ.JsonConverter<ConcurrentMultimap<TKey, TValue>>
{
    public override ConcurrentMultimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TODO: (json) (mc) Polymorphy?
        var list = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>>(ref reader, options);

        return typeof(TKey) == typeof(string)
            ? new ConcurrentMultimap<TKey, TValue>(list, (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase)
            : new ConcurrentMultimap<TKey, TValue>(list);
    }

    public override void Write(Utf8JsonWriter writer, ConcurrentMultimap<TKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, item.GetType(), options);
        }
        writer.WriteEndArray();
    }
}

#endregion
