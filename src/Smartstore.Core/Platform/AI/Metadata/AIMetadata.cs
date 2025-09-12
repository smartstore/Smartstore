#nullable enable

using Newtonsoft.Json;

namespace Smartstore.Core.AI.Metadata
{
    /// <summary>
    /// Root object for metadata.json.
    /// </summary>
    public class AIMetadata
    {
        #region Properties

        /// <summary>
        /// Arbitrary config version or timestamp string.
        /// </summary>
        public string Version { get; set; } = default!;

        /// <summary>
        /// Internal provider ID (e.g. "openai").
        /// </summary>
        public string ProviderId { get; set; } = default!;

        /// <summary>
        /// Human-readable provider name.
        /// </summary>
        public string? ProviderName { get; set; }

        /// <summary>
        /// Specifies the capabilities of this provider (e.g. text generation, image generation, translation etc.).
        /// </summary>
        [JsonConverter(typeof(AIProviderFeaturesConverter))]
        public AIProviderFeatures Capabilities { get; set; }

        /// <summary>
        /// List of LLM models available under this provider.
        /// </summary>
        public IReadOnlyList<AIModelEntry> Models { get; set; } = [];

        #endregion

        #region Supports...

        public bool Supports(AIProviderFeatures feature)
            => Capabilities.HasFlag(feature);

        public bool SupportsTextGeneration
            => Supports(AIProviderFeatures.TextGeneration);

        public bool SupportsTranslation
            => Supports(AIProviderFeatures.Translation);

        public bool SupportsImageGeneration
            => Supports(AIProviderFeatures.ImageGeneration);

        public bool SupportsImageAnalysis
            => Supports(AIProviderFeatures.ImageAnalysis);

        public bool SuportsThemeVarGeneration
            => Supports(AIProviderFeatures.ThemeVarGeneration);

        public bool SupportsAssistance
            => Supports(AIProviderFeatures.Assistance);

        #endregion

        #region Query models

        /// <summary>
        /// Gets the preferred model for the given topic.
        /// </summary>
        public AIModelEntry? GetPreferredModel(AIChatTopic topic)
            => GetPreferredModel(topic == AIChatTopic.Image ? AIOutputType.Image : AIOutputType.Text);

        /// <summary>
        /// Gets the preferred model for the given output type.
        /// </summary>
        public AIModelEntry? GetPreferredModel(AIOutputType outputType)
        {
            return Models.FirstOrDefault(x => x.Preferred && x.Type == outputType && !x.Deprecated)
                ?? Models.FirstOrDefault(x => x.Type == outputType && !x.Deprecated);
        }

        /// <summary>
        /// Gets all text models.
        /// </summary>
        public IEnumerable<AIModelEntry> GetTextModels()
        {
            return Models.Where(x => x.Type == AIOutputType.Text && !x.Deprecated).OrderByDescending(x => x.Preferred);
        }

        /// <summary>
        /// Gets all image models.
        /// </summary>
        public IEnumerable<AIModelEntry> GetImageModels()
        {
            return Models.Where(x => x.Type == AIOutputType.Image && !x.Deprecated).OrderByDescending(x => x.Preferred);
        }

        /// <summary>
        /// Gets a model by its ID.
        /// </summary>
        /// <param name="mapDeprecated">If true, tries to resolve deprecated models to their alias.</param>
        public AIModelEntry? GetModelById(string modelId, bool mapDeprecated = true)
        {
            var model = Models.FirstOrDefault(x => x.Id == modelId);
            if (model != null && model.Deprecated && model.Alias.HasValue() && mapDeprecated)
            {
                // Try to resolve alias
                model = Models.FirstOrDefault(x => x.Id == model.Alias);
            }

            return model;
        }

        #endregion
    }
}
