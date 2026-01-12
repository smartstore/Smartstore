using System.Collections;
using System.Text.Json;
using NSJ = Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;

namespace Smartstore.Collections.JsonConverters
{
    internal sealed class ConcurrentMultiMapConverter : MultiMapJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ConcurrentMultimap<,>);
            return canConvert;
        }
    }

    internal sealed class ConcurrentMultiMapSTJConverter : MultiMapStjConverter
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ConcurrentMultimap<,>);
        }
    }

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

    internal class MultiMapStjConverter : STJ.JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // typeof TKey
            var keyType = typeToConvert.GetGenericArguments()[0];

            // typeof TValue
            var valueType = typeToConvert.GetGenericArguments()[1];

            // typeof IEnumerable<KeyValuePair<TKey, ICollection<TValue>>
            var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, typeof(IEnumerable<>).MakeGenericType(valueType));
            var sequenceType = typeof(List<>).MakeGenericType(kvpType);

            // Deserialize the array
            var list = JsonSerializer.Deserialize(ref reader, sequenceType, options);

            if (keyType == typeof(string))
            {
                // call constructor Multimap(IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> items, IEqualityComparer<TKey> comparer)
                // TBD: we always assume string keys to be case insensitive. Serialize it somehow and fetch here!
                return Activator.CreateInstance(typeToConvert, [list, StringComparer.OrdinalIgnoreCase]);
            }
            else
            {
                // call constructor Multimap(IEnumerable<KeyValuePair<TKey, ICollection<TValue>>> items)
                return Activator.CreateInstance(typeToConvert, [list]);
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            {
                var enumerable = value as IEnumerable;
                foreach (var item in enumerable)
                {
                    JsonSerializer.Serialize(writer, item, item.GetType(), options);
                }
            }
            writer.WriteEndArray();
        }
    }

    internal class MultiMapStjConverterFactory : STJ.JsonConverterFactory
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
                converterType = typeof(ConcurrentMultiMapStjConverterInner<,>)
                    .MakeGenericType(keyType, valueType);
            }
            else
            {
                converterType = typeof(MultiMapStjConverterInner<,>)
                    .MakeGenericType(keyType, valueType);
            }

            return (STJ.JsonConverter)Activator.CreateInstance(converterType);
        }

        private class MultiMapStjConverterInner<TKey, TValue> : STJ.JsonConverter<Multimap<TKey, TValue>>
        {
            public override Multimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var keyType = typeof(TKey);
                var valueType = typeof(TValue);

                var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, typeof(IEnumerable<>).MakeGenericType(valueType));
                var sequenceType = typeof(List<>).MakeGenericType(kvpType);

                var list = JsonSerializer.Deserialize(ref reader, sequenceType, options);

                if (keyType == typeof(string))
                {
                    return (Multimap<TKey, TValue>)Activator.CreateInstance(typeToConvert, new object[] { list, StringComparer.OrdinalIgnoreCase });
                }
                else
                {
                    return (Multimap<TKey, TValue>)Activator.CreateInstance(typeToConvert, new object[] { list });
                }
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

        private class ConcurrentMultiMapStjConverterInner<TKey, TValue> : STJ.JsonConverter<ConcurrentMultimap<TKey, TValue>>
        {
            public override ConcurrentMultimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var keyType = typeof(TKey);
                var valueType = typeof(TValue);

                var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, typeof(IEnumerable<>).MakeGenericType(valueType));
                var sequenceType = typeof(List<>).MakeGenericType(kvpType);

                var list = JsonSerializer.Deserialize(ref reader, sequenceType, options);

                if (keyType == typeof(string))
                {
                    return (ConcurrentMultimap<TKey, TValue>)Activator.CreateInstance(typeToConvert, new object[] { list, StringComparer.OrdinalIgnoreCase });
                }
                else
                {
                    return (ConcurrentMultimap<TKey, TValue>)Activator.CreateInstance(typeToConvert, new object[] { list });
                }
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
    }
}