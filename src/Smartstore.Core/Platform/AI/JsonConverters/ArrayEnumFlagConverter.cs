using Newtonsoft.Json;

namespace Smartstore.Core.AI
{
    internal abstract class ArrayEnumFlagConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        protected abstract IDictionary<string, TEnum> GetMapping();

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, TEnum value, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override TEnum ReadJson(JsonReader reader, Type objectType, TEnum existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var mapping = GetMapping();
            
            if (reader.TokenType == JsonToken.String && reader.Value is string str && mapping.TryGetValue(str, out var flag))
            {
                return flag;
            }

            var flags = new TEnum();

            if (reader.TokenType != JsonToken.StartArray)
            {
                return flags;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonToken.String && reader.Value is string str2)
                {
                    if (mapping.TryGetValue(str2, out flag))
                    {
                        flags = (TEnum)(object)((int)(object)flags | (int)(object)flag);
                    }
                }
            }

            return flags;
        }
    }
}
