using System.Text.Json;
using Smartstore.Json.Polymorphy;
using System.Text.Json.Serialization;

namespace Smartstore.Core.AI
{
    internal sealed class AIChatJsonConverter : JsonConverter<AIChat>
    {
        public override AIChat Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            IReadOnlyList<AIChatMessage> messages = null;
            var topic = AIChatTopic.Text;
            string modelName = null;

            IDictionary<string, object> metadata = null;
            var initialUserMessageHash = 0;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                var propertyName = reader.GetString();
                reader.Read();

                if (string.Equals(propertyName, nameof(AIChat.Topic), StringComparison.OrdinalIgnoreCase))
                {
                    topic = JsonSerializer.Deserialize<AIChatTopic>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AIChat.ModelName), StringComparison.OrdinalIgnoreCase))
                {
                    modelName = reader.GetString();
                }
                else if (string.Equals(propertyName, nameof(AIChat.Messages), StringComparison.OrdinalIgnoreCase))
                {
                    messages = JsonSerializer.Deserialize<IReadOnlyList<AIChatMessage>>(ref reader, options);
                }
                else if (string.Equals(propertyName, nameof(AIChat.Metadata), StringComparison.OrdinalIgnoreCase))
                {
                    //metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
                    metadata = options.DeserializePolymorphic<IDictionary<string, object>>(ref reader);
                }
                else if (string.Equals(propertyName, nameof(AIChat.InitialUserMessage) + "Hash", StringComparison.OrdinalIgnoreCase))
                {
                    initialUserMessageHash = reader.GetInt32();
                }
                else
                {
                    reader.Skip();
                }
            }

            var chat = (AIChat)Activator.CreateInstance(typeToConvert, topic);
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

        public override void Write(Utf8JsonWriter writer, AIChat value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName(nameof(AIChat.Topic));
                JsonSerializer.Serialize(writer, value.Topic, options);

                writer.WriteString(
                    nameof(AIChat.ModelName), 
                    value.ModelName);

                writer.WritePropertyName(nameof(AIChat.Messages));
                JsonSerializer.Serialize(writer, value.Messages, options);

                var initialMessageHash = value.InitialUserMessage?.GetHashCode() ?? 0;
                if (initialMessageHash != 0)
                {
                    writer.WriteNumber(
                        nameof(AIChat.InitialUserMessage) + "Hash",
                        initialMessageHash);
                }

                if (value.Metadata is IDictionary<string, object> dict && dict.Count > 0)
                {
                    writer.WritePropertyName(nameof(AIChat.Metadata));
                    //JsonSerializer.Serialize(writer, dict, options);
                    options.SerializePolymorphic(writer, dict);
                }
            }

            writer.WriteEndObject();
        }
    }
}