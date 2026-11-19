#nullable enable

using Smartstore.IconKit.Sources;

namespace Smartstore.IconKit.Sprites;

/// <summary>
/// Plans kit and library sprites and the browser manifest without publishing files.
/// </summary>
public interface IIconSpritePlanner
{
    /// <summary>
    /// Gets the resolved kit index for a library and variant, or null if the selection is unknown.
    /// </summary>
    IconKitIndex? GetIndex(IconCatalog catalog, string? library, string? variant);

    /// <summary>
    /// Gets the sprite plan for a library variant, or null for an unknown selection.
    /// </summary>
    IconSprite? GetLibrarySprite(IconCatalog catalog, string libraryName, string variantName);

    /// <summary>
    /// Builds the browser manifest in memory and calculates its revision. Does not write cache files.
    /// </summary>
    IconKitManifest GetManifest(IconCatalog catalog);
}

/// <summary>
/// Contains the browser manifest and the revision used in its URL.
/// </summary>
/// <param name="Revision">The hash of the manifest content.</param>
/// <param name="Content">The JSON content ready to write to the cache.</param>
public sealed record IconKitManifest(string Revision, byte[] Content);
