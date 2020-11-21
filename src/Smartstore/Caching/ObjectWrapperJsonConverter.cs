using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.ComponentModel;

namespace Smartstore.Caching
{
    public class ObjectWrapperJsonConverter : JsonConverter 
    {
        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IObjectContainer).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = (IObjectContainer)(existingValue ?? Activator.CreateInstance(objectType));
            Populate(result, reader, serializer);

            return result;
        }

        protected virtual IObjectContainer Populate(IObjectContainer result, JsonReader reader, JsonSerializer serializer)
        {
            serializer.Populate(reader, result);

            var valueJson = (result.Value as JToken).ToString(Formatting.None);

            JsonConverter converter = null;

            if (result.ValueType.TryGetAttribute<JsonConverterAttribute>(true, out var converterAttribute))
            {
                converter = (JsonConverter)Activator.CreateInstance(converterAttribute.ConverterType);
            }

            if (converter == null)
            {
                converter = serializer.ContractResolver.ResolveContract(result.ValueType).Converter;
            }

            result.Value = converter != null
                ? JsonConvert.DeserializeObject(valueJson, result.ValueType, converter)
                : JsonConvert.DeserializeObject(valueJson, result.ValueType);

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}
