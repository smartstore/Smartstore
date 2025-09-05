#nullable enable

namespace Smartstore.AI.Metadata
{
    /// <summary>
    /// Represents AI provider metadata.
    /// </summary>
    public class AIProviderMetadata
    {
        /// <summary>
        /// Internal provider ID (e.g. "openai").
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Human-readable provider name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// List of LLM models available under this provider.
        /// </summary>
        public IReadOnlyList<AIModelEntry> Models { get; set; } = [];
    }
}
