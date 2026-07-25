#nullable enable

using System.ComponentModel;
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
    public string? Default { get; set; }

    /// <summary>
    /// The supported effort levels, e.g. low, medium, high.
    /// </summary>
    public string[] Levels { get; set; } = ["low", "medium", "high"];

    /// <summary>
    /// Indicates whether the model supports explicit reasoning effort control.
    /// </summary>
    [JsonIgnore]
    public bool IsSupported => Levels != null && Levels.Length > 0;

    /// <summary>
    /// Determines whether the specified effort level is supported.
    /// </summary>
    /// <param name="effort">The effort level to validate (case-insensitive).</param>
    public bool ContainsLevel(string? effort)
        => effort.HasValue() && Levels != null && Levels.Contains(effort, StringComparer.OrdinalIgnoreCase);
}
