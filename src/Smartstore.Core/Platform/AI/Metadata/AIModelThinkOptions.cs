#nullable enable

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json;
using Smartstore.Json;

namespace Smartstore.Core.AI.Metadata;

/// <summary>
/// Represents the adjustable reasoning effort levels supported by a model.
/// If <c>null</c>, the model does not support explicit reasoning effort control.
/// </summary>
public class AIModelThinkOptions : IDefaultable
{
    /// <summary>
    /// The default effort level when the user does not make an explicit choice.
    /// Defaults to <c>"auto"</c> if not specified in metadata.json.
    /// </summary>
    [DefaultValue("auto")]
    public AIReasoningEffort Default { get; set; } = AIReasoningEffort.Auto;

    /// <summary>
    /// The supported effort levels, e.g. low, medium, high.
    /// </summary>
    [DefaultValue("[]")]
    public AIReasoningEffort[] Levels { get; set; } =
    [
        AIReasoningEffort.Low,
        AIReasoningEffort.Medium,
        AIReasoningEffort.High
    ];

    /// <summary>
    /// Gets a value indicating whether the current instance is in its default state, meaning all properties are either null or empty.
    /// </summary>
    [IgnoreDataMember]
    public bool IsDefaultState
    {
        // If Levels is null or empty, "Default" is considered to be in its default state as well.
        get => Levels.IsNullOrEmpty();
    }

    /// <summary>
    /// Indicates whether the model supports explicit reasoning effort control.
    /// </summary>
    [IgnoreDataMember]
    public bool IsSupported => Levels is { Length: > 0 };

    /// <summary>
    /// Determines whether the specified effort level is supported.
    /// </summary>
    /// <param name="effort">The effort level to validate (case-insensitive).</param>
    public bool ContainsLevel(AIReasoningEffort effort)
        => Levels?.Contains(effort) == true;

    #region Serialization

    /// <summary>
    /// Serializes this instance to a JSON string.
    /// </summary>
    public virtual string? ToJson()
        => IsDefaultState ? null : JsonSerializer.Serialize(this, SmartJsonOptions.CamelCasedIgnoreDefaults);

    /// <summary>
    /// Deserializes an <see cref="AIModelThinkOptions"/> instance from a JSON string.
    /// </summary>
    public static AIModelThinkOptions? FromJson(string json)
        => json.HasValue() ? JsonSerializer.Deserialize<AIModelThinkOptions>(json, SmartJsonOptions.CamelCasedIgnoreDefaults) : null;

    #endregion
}