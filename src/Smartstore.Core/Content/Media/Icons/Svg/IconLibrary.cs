#nullable enable

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Describes a library.json manifest and its containing directory.
/// </summary>
public sealed record IconLibrary
{
    private readonly IReadOnlyDictionary<string, IconVariant> _variants =
        new ReadOnlyDictionary<string, IconVariant>(new OrderedDictionary<string, IconVariant>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the library directory name.
    /// </summary>
    [JsonIgnore]
    public string SystemName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional icon address representing this library in pickers. Null uses the generic library icon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the installed library version.
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Gets an optional unique short name used in canonical addresses.
    /// </summary>
    public string? ShortName { get; init; }

    /// <summary>
    /// Gets the default variant name.
    /// </summary>
    public string DefaultVariant { get; init; } = string.Empty;

    /// <summary>
    /// Gets locally available variants in definition order, keyed by name.
    /// </summary>
    public IReadOnlyDictionary<string, IconVariant> Variants
    {
        get => _variants;
        init
        {
            Guard.NotNull(value);

            // Take immutable ownership at initialization. A read-only interface over a caller's
            // mutable dictionary would still allow the published manifest to change indirectly.
            // FrozenDictionary may reorder entries; the picker follows the manifest's order.
            _variants = new ReadOnlyDictionary<string, IconVariant>(
                new OrderedDictionary<string, IconVariant>(value, StringComparer.OrdinalIgnoreCase));
        }
    }
}

/// <summary>
/// Describes a local icon variant and optional paint defaults.
/// </summary>
public sealed record IconVariant
{
    private readonly IReadOnlyList<string> _fallbacks = Array.Empty<string>();

    /// <summary>
    /// Gets ordered fallback variant names or short names within this library.
    /// Chains are followed depth-first, visiting each variant at most once.
    /// </summary>
    public IReadOnlyList<string> Fallbacks
    {
        get => _fallbacks;
        init
        {
            Guard.NotNull(value);

            _fallbacks = Array.AsReadOnly(value.ToArray());
        }
    }

    /// <summary>
    /// Gets the variant directory name.
    /// </summary>
    [JsonIgnore]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional display name for variant selection. Null when no display name is configured.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the optional icon address representing this variant in pickers. Null inherits the library icon.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets an optional short name, unique within its library.
    /// </summary>
    public string? ShortName { get; init; }

    /// <summary>
    /// Gets the optional viewBox fallback used only when the SVG has no viewBox attribute.
    /// </summary>
    public string? DefaultViewBox { get; init; }

    /// <summary>
    /// Gets the optional fill override. Null preserves source paint.
    /// </summary>
    public string? Fill { get; init; }

    /// <summary>
    /// Gets the optional stroke override. Null preserves source paint.
    /// </summary>
    public string? Stroke { get; init; }

    /// <summary>
    /// Gets the multiplier applied once to source stroke widths. Defaults to 1 (unchanged).
    /// </summary>
    public double StrokeWidthScale { get; init; } = 1;
}
