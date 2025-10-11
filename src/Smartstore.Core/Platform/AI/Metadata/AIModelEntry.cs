#nullable enable

using System.ComponentModel;
using Newtonsoft.Json;

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
    /// Represents the performance level of an AI model.
    /// </summary>
    public enum AIModelPerformanceLevel
    {
        Fast,
        Balanced,
        DeepReasoning
    }

    /// <summary>
    /// Represents a single LLM model entry in the catalog.
    /// </summary>
    public class AIModelEntry : ICloneable<AIModelEntry>, IEquatable<AIModelEntry>
    {
        /// <summary>
        /// Model identifier (e.g. "gpt-5", "gemini-2.5-pro").
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Model name (e.g. "GPT-5", "Gemini 2.5 Pro").
        /// </summary>
        public string? Name { get; set; }

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

        /// <summary>
        /// The performance level of the model.
        /// </summary>
        [DefaultValue(AIModelPerformanceLevel.Balanced)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public AIModelPerformanceLevel Level { get; set; }

        /// <summary>
        /// Indicates whether the model supports streaming responses.
        /// </summary>
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool Stream { get; set; }

        /// <summary>
        /// User-defined custom model (not provided by metadata.json).
        /// </summary>
        public bool IsCustom { get; set; }

        /// <inheritdoc/>
        public AIModelEntry Clone()
            => (AIModelEntry)MemberwiseClone();

        /// <inheritdoc/>
        object ICloneable.Clone()
            => MemberwiseClone();

        public override string ToString()
            => $"{Id}, Preferred: {Preferred}";

        public override bool Equals(object? other)
            => Equals(other as AIModelEntry);

        public bool Equals(AIModelEntry? other)
            => other != null && Id == other.Id;

        public override int GetHashCode()
            => Id.GetHashCode();
    }
}
