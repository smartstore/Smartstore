#nullable enable

namespace Smartstore.Core.AI.Metadata
{
    /// <summary>
    /// Represents the type of AI output.
    /// </summary>
    public enum AIOutputType
    {
        Text,
        Image
    }

    /// <summary>
    /// Represents a single LLM model entry in the catalog.
    /// </summary>
    public class AIModelEntry
    {
        /// <summary>
        /// Model identifier (e.g. "gpt-5", "gemini-2.5-pro").
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Model type: "text" or "image".
        /// </summary>
        public AIOutputType Type { get; set; }

        /// <summary>
        /// Human-readable description of the model.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether this model should be shown as recommended in the UI.
        /// </summary>
        public bool Preferred { get; set; }

        /// <summary>
        /// Has vision (image analysis) capabilities.
        /// </summary>
        public bool Vision { get; set; }

        /// <summary>
        /// Marks outdated models that should not be offered to users.
        /// </summary>
        public bool Deprecated { get; set; }

        /// <summary>
        /// Suggested replacement model ID for deprecated models.
        /// </summary>
        public string? Alias { get; set; }
    }
}
