using System.Text.Json;
using Smartstore.Json.Polymorphy;
using NSJ = Newtonsoft.Json;
using STJ = System.Text.Json.Serialization;

namespace Smartstore.Core.AI
{
    internal sealed class AIChatJsonConverter : NSJ.JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(AIChat);

        public override object ReadJson(NSJ.JsonReader reader, Type objectType, object existingValue, NSJ.JsonSerializer serializer)
        {
            IReadOnlyList<AIChatMessage> messages = null;
            var topic = AIChatTopic.Text;
            string modelName = null;
            Dictionary<string, object> metadata = null;
            var initialUserMessageHash = 0;

            reader.Read();
            while (reader.TokenType == NSJ.JsonToken.PropertyName)
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
                    messages = serializer.Deserialize<IReadOnlyList<AIChatMessage>>(reader);
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

        public override void WriteJson(NSJ.JsonWriter writer, object value, NSJ.JsonSerializer serializer)
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

    internal sealed class AIChatStjConverter : STJ.JsonConverter<AIChat>
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