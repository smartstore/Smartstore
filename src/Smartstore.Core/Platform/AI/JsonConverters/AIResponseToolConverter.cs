using Smartstore.Core.AI.Metadata;
using Smartstore.Json.Converters;

namespace Smartstore.Core.AI;

internal sealed class AIResponseToolConverter : ArrayEnumFlagConverter<AIResponseTool>
{
    protected override IReadOnlyDictionary<string, AIResponseTool> GetMapping()
    {
        return new Dictionary<string, AIResponseTool>(StringComparer.OrdinalIgnoreCase)
        {
            ["WebSearch"] = AIResponseTool.WebSearch,
            ["ImageGeneration"] = AIResponseTool.ImageGeneration,
            ["CodeAnalysis"] = AIResponseTool.CodeAnalysis,
            ["FileSearch"] = AIResponseTool.FileSearch
        };
    }
}