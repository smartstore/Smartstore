namespace Smartstore.Core.AI
{
    internal sealed class AIProviderFeaturesConverter : ArrayEnumFlagConverter<AIProviderFeatures>
    {
        protected override IDictionary<string, AIProviderFeatures> GetMapping()
        {
            return new Dictionary<string, AIProviderFeatures>(StringComparer.OrdinalIgnoreCase)
            {
                ["text"] = AIProviderFeatures.TextGeneration,
                ["translation"] = AIProviderFeatures.Translation,
                ["image"] = AIProviderFeatures.ImageGeneration,
                ["vision"] = AIProviderFeatures.ImageAnalysis,
                ["theme"] = AIProviderFeatures.ThemeVarGeneration,
                ["assistant"] = AIProviderFeatures.Assistance
            };
        }
    }
}