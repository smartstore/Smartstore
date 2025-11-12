using Smartstore.Core.AI.Metadata;

namespace Smartstore.Core.AI
{
    internal sealed class AIResponseToolConverter : ArrayEnumFlagConverter<AIResponseTool>
    {
        protected override IDictionary<string, AIResponseTool> GetMapping()
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
}