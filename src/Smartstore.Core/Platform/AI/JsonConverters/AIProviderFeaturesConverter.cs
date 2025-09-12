using Newtonsoft.Json;

namespace Smartstore.Core.AI
{
    public class AIProviderFeaturesConverter : JsonConverter<AIProviderFeatures>
    {
        private static readonly Dictionary<string, AIProviderFeatures> _mapping = new(StringComparer.OrdinalIgnoreCase)
        {
            ["text"] = AIProviderFeatures.TextGeneration,
            ["translation"] = AIProviderFeatures.Translation,
            ["image"] = AIProviderFeatures.ImageGeneration,
            ["vision"] = AIProviderFeatures.ImageAnalysis,
            ["theme"] = AIProviderFeatures.ThemeVarGeneration,
            ["assistant"] = AIProviderFeatures.Assistance
        };

        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, AIProviderFeatures value, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override AIProviderFeatures ReadJson(JsonReader reader, Type objectType, AIProviderFeatures existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String && reader.Value is string str && _mapping.TryGetValue(str, out var flag))
            {
                return flag;
            }

            var capabilities = AIProviderFeatures.None;

            if (reader.TokenType != JsonToken.StartArray)
            {
                return capabilities;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonToken.String && reader.Value is string str2)
                {
                    if (_mapping.TryGetValue(str2, out flag))
                    {
                        capabilities |= flag;
                    }
                }
            }

            return capabilities;
        }
    }
}