#nullable enable

namespace Smartstore.AI.Metadata
{
    /// <summary>
    /// Root object for metadata.json.
    /// </summary>
    public class AIMetadata
    {
        /// <summary>
        /// Arbitrary config version or timestamp string.
        /// </summary>
        public string Version { get; set; } = default!;

        /// <summary>
        /// Default model IDs for text and image operations.
        /// </summary>
        public AIModelDefaults Defaults { get; set; } = default!;

        /// <summary>
        /// Single provider configuration (used by provider-specific files).
        /// Optional.
        /// </summary>
        public AIProviderMetadata Provider { get; set; } = default!;
    }
}
