using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace Smartstore.Collections.JsonConverters
{
    internal sealed class MultiMapJsonConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Multimap<,>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            // typeof TKey
            var keyType = typeToConvert.GetGenericArguments()[0];

            // typeof TValue
            var valueType = typeToConvert.GetGenericArguments()[1];

            //// typeof List<KeyValuePair<TKey, ICollection<TValue>>
            //var sequenceType = typeof(List<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, typeof(ICollection<>).MakeGenericType(valueType)));

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(MultiMapConverterInner<,>).MakeGenericType(new Type[] { keyType, valueType }),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null);

            return converter;
        }

        class MultiMapConverterInner<TKey, TValue> : JsonConverter<Multimap<TKey, TValue>>
        {
            private readonly JsonConverter<TKey> _keyConverter;
            private readonly JsonConverter<ICollection<TValue>> _valueConverter;
            private readonly JsonConverter<KeyValuePair<TKey, ICollection<TValue>>> _kvpConverter;
            private readonly JsonConverter<IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>> _listConverter;

            public MultiMapConverterInner(JsonSerializerOptions options)
            {
                _keyConverter = (JsonConverter<TKey>)options.GetConverter(typeof(TKey));
                _valueConverter = (JsonConverter<ICollection<TValue>>)options.GetConverter(typeof(ICollection<TValue>));
                _kvpConverter = (JsonConverter<KeyValuePair<TKey, ICollection<TValue>>>)options.GetConverter(typeof(KeyValuePair<TKey, ICollection<TValue>>));
                _listConverter = (JsonConverter<IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>>)options.GetConverter(typeof(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>));
            }

            public override bool CanConvert(Type typeToConvert)
                => true;

            public override Multimap<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var items = _listConverter.Read(ref reader, typeof(IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>), options);

                Multimap<TKey, TValue> map = typeof(TKey) == typeof(string) 
                    ? new(items, (IEqualityComparer<TKey>)StringComparer.OrdinalIgnoreCase) 
                    : new(items);
                
                return map;
            }

            public override void Write(Utf8JsonWriter writer, Multimap<TKey, TValue> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                {
                    foreach (var item in value)
                    {
                        _kvpConverter.Write(writer, item, options);
                    }
                }
                writer.WriteEndArray();
            }
        }
    }
}