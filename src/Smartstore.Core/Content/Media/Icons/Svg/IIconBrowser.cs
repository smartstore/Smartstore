#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Searches picker sources and prepares complete preview sprites without populating the individual icon cache.
/// </summary>
public interface IIconBrowser
{
    /// <summary>
    /// Gets a page of concepts or literal icon names and ensures its preview sprite exists.
    /// </summary>
    /// <param name="query">Search terms, library/variant selection and page boundaries.</param>
    /// <param name="kit">An optional kit name. When supplied, library and variant selectors must be absent.</param>
    /// <returns>The page, or null for an unknown or excluded source.</returns>
    Task<IconBrowserResult?> SearchAsync(IconSearchQuery query, string? kit = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Gets an existing variant sprite or generates its current revision on demand.
    /// </summary>
    /// <param name="library">The registered library name or short name.</param>
    /// <param name="variant">The registered variant name or short name.</param>
    /// <param name="revision">The requested immutable sprite revision.</param>
    /// <returns>The physical file path, or null for an unknown source or unavailable historical revision.</returns>
    Task<string?> GetSpriteFileAsync(string library, string variant, string revision, CancellationToken cancelToken = default);
}

/// <summary>
/// Describes one picker page and the shared external sprite for its previews.
/// </summary>
/// <param name="SpriteUrl">The revisioned sprite URL, including the application's path base.</param>
/// <param name="TotalCount">The matching count before pagination.</param>
/// <param name="Items">The requested page of concepts or literal icons.</param>
public sealed record IconBrowserResult(string SpriteUrl, int TotalCount, IReadOnlyList<IconBrowserItem> Items);

/// <summary>
/// Describes one picker option without carrying SVG artwork or search metadata.
/// </summary>
/// <param name="Name">The displayed concept or literal icon name, also used as the sprite symbol ID.</param>
/// <param name="Value">The concept name or explicitly qualified direct address to persist.</param>
/// <param name="Address">The canonical source address used for DOM inspection.</param>
/// <param name="LibraryKey">The short or system library name used for CSS classes.</param>
/// <param name="VariantKey">The short or system variant name used for CSS classes.</param>
/// <param name="InlineName">An explicit address with modifiers for exceptions that require inline rendering.</param>
public sealed record IconBrowserItem(string Name, string Value, string Address, string LibraryKey, string VariantKey, string? InlineName = null);
