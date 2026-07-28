using Smartstore.Json.Converters;

namespace Smartstore.Core.AI;

public sealed class AIProviderFeaturesConverter : ArrayEnumFlagConverter<AIProviderFeatures>
{
    protected override IReadOnlyDictionary<string, AIProviderFeatures> GetMapping()
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