using Newtonsoft.Json;

namespace Smartstore.Core.AI
{
    internal sealed class AIChatJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(AIChat);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IReadOnlyList<AIChatMessage> messages = null;
            var topic = AIChatTopic.Text;
            string modelName = null;

            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
            {
                var name = reader.Value.ToString();

                if (string.Equals(name, nameof(AIChat.Topic), StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    topic = serializer.Deserialize<AIChatTopic>(reader);
                }
                else if (string.Equals(name, nameof(AIChat.ModelName), StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    modelName = serializer.Deserialize<string>(reader);
                }
                else if (string.Equals(name, nameof(AIChat.Messages), StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    messages = serializer.Deserialize(reader, typeof(IReadOnlyList<AIChatMessage>)) as IReadOnlyList<AIChatMessage>;
                }
                else
                {
                    reader.Skip();
                }

                reader.Read();
            }

            var chat = (AIChat)Activator.CreateInstance(objectType, topic);
            chat.UseModel(modelName)
                .AddMessages([.. messages]);

            return chat;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(AIChat.Topic));
                serializer.Serialize(writer, GetPropValue(nameof(AIChat.Topic), value));

                writer.WritePropertyName(nameof(AIChat.ModelName));
                serializer.Serialize(writer, GetPropValue(nameof(AIChat.ModelName), value));

                writer.WritePropertyName(nameof(AIChat.Messages));
                serializer.Serialize(writer, GetPropValue(nameof(AIChat.Messages), value));
            }
            writer.WriteEndObject();
        }

        private static object GetPropValue(string name, object instance)
        {
            return instance.GetType().GetProperty(name).GetValue(instance);
        }
    }
}
