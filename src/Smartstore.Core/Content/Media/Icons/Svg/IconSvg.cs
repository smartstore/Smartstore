#nullable enable

using System.Collections.Frozen;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// An immutable, serializable SVG payload shared across cache consumers.
/// </summary>
public sealed class IconSvg
{
    private readonly FrozenDictionary<string, string> _rootAttributes = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the canonical address after mapping.
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source and preparation revision.
    /// </summary>
    public string Revision { get; init; } = string.Empty;

    /// <summary>
    /// Gets the library system name.
    /// </summary>
    public string Library { get; init; } = string.Empty;

    /// <summary>
    /// Gets the variant name.
    /// </summary>
    public string Variant { get; init; } = string.Empty;

    /// <summary>
    /// Gets the actual icon name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the SVG coordinate rectangle, for example "0 0 24 24".
    /// </summary>
    public string ViewBox { get; init; } = string.Empty;

    /// <summary>
    /// Gets root attributes excluding xmlns, viewBox, width and height.
    /// </summary>
    public IReadOnlyDictionary<string, string> RootAttributes
    {
        get => _rootAttributes;
        init
        {
            Guard.NotNull(value);

            // Freeze once at construction/deserialization, never on a cache read or write.
            // Retaining a caller-owned mutable dictionary would invalidate immutability.
            _rootAttributes = value.ToFrozenDictionary(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Gets validated SVG child markup. Contains no outer svg element.
    /// </summary>
    public string Content { get; init; } = string.Empty;

}

/// <summary>
/// Describes an available icon after conceptual name mapping.
/// </summary>
public sealed class IconInfo
{
    private string[]? _tags;

    /// <summary>
    /// Gets whether address modifiers replace a transformation already baked into a kit.
    /// </summary>
    internal bool RequiresInline { get; set; }

    /// <summary>
    /// Gets or sets the requested library context for kit membership before artwork fallbacks.
    /// Null uses the actual library, as for native search results.
    /// </summary>
    internal string? SelectionLibraryName { get; set; }

    /// <summary>
    /// Gets or sets the requested variant context for kit membership before artwork fallbacks.
    /// Null uses the actual variant, as for native search results.
    /// </summary>
    internal string? SelectionVariantName { get; set; }

    /// <summary>
    /// Gets or sets the per-icon stroke multiplier applied after the library multiplier.
    /// </summary>
    public double StrokeScale { get; set; } = 1;

    /// <summary>
    /// Loads supplemental metadata on first access so SVG-only lookups do not read metadata.json.
    /// </summary>
    internal Lazy<string[]>? DeferredTags { get; init; }

    /// <summary>
    /// Gets or sets the mapping transformation; direct addresses and search results use identity.
    /// </summary>
    public IconTransform Transform { get; set; }

    /// <summary>
    /// Gets or sets the canonical address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the library system name.
    /// </summary>
    public string LibraryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional library short name used for CSS selectors.
    /// </summary>
    public string? LibraryShortName { get; set; }

    /// <summary>
    /// Gets the library short name when configured, otherwise its system name.
    /// </summary>
    public string LibraryKey => LibraryShortName ?? LibraryName;

    /// <summary>
    /// Gets or sets the variant name.
    /// </summary>
    public string VariantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional variant short name used for CSS selectors.
    /// </summary>
    public string? VariantShortName { get; set; }

    /// <summary>
    /// Gets the variant short name when configured, otherwise its name.
    /// </summary>
    public string VariantKey => VariantShortName ?? VariantName;

    /// <summary>
    /// Gets or sets the actual icon name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets supplemental English search tags.
    /// </summary>
    public string[] Tags
    {
        get => _tags ?? DeferredTags?.Value ?? [];
        set => _tags = value;
    }
}

/// <summary>
/// Filters and pages available icons in one library and variant.
/// </summary>
public sealed class IconSearchQuery
{
    /// <summary>
    /// Gets or sets words matched against the name and tags.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Gets or sets a library system name or short name; null selects the default.
    /// </summary>
    public string? Library { get; set; }

    /// <summary>
    /// Gets or sets a variant; null selects its effective default.
    /// </summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Gets or sets the number of matching icons to skip.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the page size (1 to 500).
    /// </summary>
    public int Take { get; set; } = 50;
}

/// <summary>
/// A page of icon search results.
/// </summary>
public sealed class IconSearchResult
{
    /// <summary>
    /// Gets or sets the total number of matching icons.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the requested page.
    /// </summary>
    public IReadOnlyList<IconInfo> Items { get; set; } = [];
}
