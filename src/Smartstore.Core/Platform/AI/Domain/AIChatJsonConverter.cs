using Newtonsoft.Json;

namespace Smartstore.Core.AI
{
    internal sealed class AIChatJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(AIChat);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            IReadOnlyList<AIChatMessage> messages = null;
            var topic = AIChatTopic.Text;
            string modelName = null;
            Dictionary<string, object> metadata = null;
            var initialUserMessageHash = 0;

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
                else if (string.Equals(name, nameof(AIChat.Metadata), StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    metadata = serializer.Deserialize<Dictionary<string, object>>(reader);
                }
                else if (string.Equals(name, nameof(AIChat.InitialUserMessage) + "Hash", StringComparison.OrdinalIgnoreCase))
                {
                    reader.Read();
                    initialUserMessageHash = serializer.Deserialize<int>(reader);
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

            if (metadata != null && metadata.Count > 0)
            {
                chat.Metadata = metadata;
            }

            if (initialUserMessageHash != 0)
            {
                chat.InitialUserMessage = messages.FirstOrDefault(x => x.GetHashCode() == initialUserMessageHash);
            }

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

                var initialMessage = GetPropValue(nameof(AIChat.InitialUserMessage), value) as AIChatMessage;
                writer.WritePropertyName(nameof(AIChat.InitialUserMessage) + "Hash");
                serializer.Serialize(writer, initialMessage?.GetHashCode() ?? 0);

                if (GetPropValue(nameof(AIChat.Metadata), value) is IDictionary<string, object> dict && dict.Count > 0)
                {
                    writer.WritePropertyName(nameof(AIChat.Metadata));
                    serializer.SerializeObjectDictionary(writer, dict);
                }
            }
            writer.WriteEndObject();
        }

        private static object GetPropValue(string name, object instance)
            => instance.GetType().GetProperty(name).GetValue(instance);
    }
}
