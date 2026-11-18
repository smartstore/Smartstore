#nullable enable

using Smartstore.IconKit.Sources;

namespace Smartstore.IconKit.Sprites;

/// <summary>
/// Creates sprites containing all icons from one library variant.
/// </summary>
public interface IIconLibrarySpriteService
{
    /// <summary>
    /// Gets the sprite revision without writing an SVG file. Returns null for an unknown library or variant.
    /// </summary>
    string? GetRevision(IconCatalog catalog, string libraryName, string variantName);

    /// <summary>
    /// Writes the sprite if needed and returns its revision, or null for an unknown library or variant.
    /// </summary>
    Task<string?> PrepareAsync(IconCatalog catalog, string libraryName, string variantName, CancellationToken cancelToken = default);
}
