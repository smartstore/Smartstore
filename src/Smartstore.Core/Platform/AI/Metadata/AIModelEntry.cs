#nullable enable

using System.ComponentModel;
using Newtonsoft.Json;
using Smartstore.Imaging;

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
    /// Represents the tools available for AI response generation, allowing for a combination of multiple tools.
    /// </summary>
    [Flags]
    public enum AIResponseTool
    {
        None = 0,
        WebSearch = 1 << 0,
        ImageGeneration = 1 << 1,
        CodeAnalysis = 1 << 2,
        FileSearch = 1 << 3
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
        public AIModelPerformanceLevel Level { get; set; } = AIModelPerformanceLevel.Balanced;

        /// <summary>
        /// Gets or sets the tools supported by this model.
        /// </summary>
        [JsonConverter(typeof(AIResponseToolConverter))]
        public AIResponseTool Tools { get; set; }

        /// <summary>
        /// Indicates whether the model supports streaming responses.
        /// </summary>
        [DefaultValue(true)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool Stream { get; set; } = true;

        /// <summary>
        /// Gets the output capabilities of the AI image generation process.
        /// </summary>
        [JsonProperty("output")]
        public AIImageOutput? ImageOutputCapabilities { get; set; }

        /// <summary>
        /// Retrieves the list of supported aspect ratios for image output.
        /// </summary>
        public string[] GetSupportedAspectRatios()
            => ImageOutputCapabilities?.AspectRatios ?? AIImageOutput.Default.AspectRatios!;

        /// <summary>
        /// Retrieves the list of supported resolutions for image output.
        /// </summary>
        public string[] GetSupportedImageResolutions()
            => ImageOutputCapabilities?.Resolutions ?? AIImageOutput.Default.Resolutions!;

        /// <summary>
        /// Retrieves the list of supported formats for image output.
        /// </summary>
        public string[] GetSupportedImageFormats()
            => ImageOutputCapabilities?.Formats ?? AIImageOutput.Default.Formats!;

        /// <summary>
        /// User-defined custom model (not provided by metadata.json).
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// Determines whether the specified tool is supported.
        /// </summary>
        /// <param name="tool">The tool to check for support.</param>
        public bool SupportsTool(AIResponseTool tool)
            => Tools.HasFlag(tool);

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
