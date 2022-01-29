using System.Collections;
using Newtonsoft.Json;

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

    internal class MultiMapJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Multimap<,>);
            return canConvert;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
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

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
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
}