#nullable enable

using System.Text.Json.Serialization;

namespace Smartstore.Core.AI.Metadata;

/// <summary>
/// Represents the adjustable reasoning effort levels supported by a model.
/// If <c>null</c>, the model does not support explicit reasoning effort control.
/// </summary>
public class AIModelThinkOptions
{
    /// <summary>
    /// The default effort level when the user does not make an explicit choice.
    /// Defaults to <c>"auto"</c> if not specified in metadata.json.
    /// </summary>
    public AIReasoningEffort Default { get; set; } = AIReasoningEffort.Auto;

    /// <summary>
    /// The supported effort levels, e.g. low, medium, high.
    /// </summary>
    public AIReasoningEffort[] Levels { get; set; } =
    [
        AIReasoningEffort.Low,
        AIReasoningEffort.Medium,
        AIReasoningEffort.High
    ];

    /// <summary>
    /// Indicates whether the model supports explicit reasoning effort control.
    /// </summary>
    [JsonIgnore]
    public bool IsSupported => Levels is { Length: > 0 };

    /// <summary>
    /// Determines whether the specified effort level is supported.
    /// </summary>
    /// <param name="effort">The effort level to validate (case-insensitive).</param>
    public bool ContainsLevel(AIReasoningEffort effort)
        => Levels?.Contains(effort) == true;
}
