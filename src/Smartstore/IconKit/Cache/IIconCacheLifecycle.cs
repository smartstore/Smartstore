#nullable enable

using Smartstore.IconKit.Sources;
using Microsoft.Extensions.Primitives;
using Smartstore.IO;

namespace Smartstore.IconKit.Cache;

/// <summary>
/// Tracks changes to icon files and removes outdated files from this server's icon cache.
/// </summary>
public interface IIconCacheLifecycle
{
    /// <summary>
    /// Returns the icon file changes detected since the last check, or an empty list if nothing changed.
    /// </summary>
    IReadOnlyList<IconChange> CollectChanges();

    /// <summary>
    /// Starts watching the icon source files. The returned token reports changes to those files.
    /// </summary>
    IChangeToken StartWatchingSources(IFileSystem sources);

    /// <summary>
    /// Adds a generated file to the cache index. May start a cleanup that removes outdated files whose grace period has expired.
    /// Rebuilds or checks the index when needed, using the supplied catalog.
    /// Rejects the file if that catalog's sources have changed.
    /// </summary>
    Task TrackCacheFileAsync(IconCacheArtifact artifact, IconCatalog catalog, CancellationToken cancelToken = default);
}

/// <summary>
/// Describes what changed in the icon files or settings.
/// </summary>
public enum IconChangeKind
{
    /// <summary>
    /// Search tags changed.
    /// </summary>
    SearchMetadata,
    /// <summary>
    /// The mappings from icon names to library icons changed.
    /// </summary>
    Mapping,
    /// <summary>
    /// A ZIP archive or an individual SVG file changed.
    /// </summary>
    VariantSources,
    /// <summary>
    /// A library's name, variants or fallback settings changed.
    /// </summary>
    LibraryManifest,
    /// <summary>
    /// Shared icon settings, kits or collections changed.
    /// </summary>
    GlobalConfiguration
}

/// <summary>
/// Describes a change and the library or variant it affects.
/// </summary>
public sealed class IconChange(IconChangeKind kind, string? library = null, string? variant = null)
{
    /// <summary>
    /// Gets what changed.
    /// </summary>
    public IconChangeKind Kind { get; } = kind;

    /// <summary>
    /// Gets the affected library's folder name, or null if the change is not limited to one library.
    /// </summary>
    public string? Library { get; } = library;

    /// <summary>
    /// Gets the affected variant's folder name, or null if the change is not limited to one variant.
    /// </summary>
    public string? Variant { get; } = variant;
}

/// <summary>
/// Describes a generated cache file and the kit or icon variant it belongs to.
/// </summary>
/// <param name="Kind">The file type: kit sprite, browser sprite, or browser manifest (kit, browser, or manifest).</param>
/// <param name="Kit">The kit name, or null for browser sprites and manifests.</param>
/// <param name="Library">The preferred library for kits, or the source library for browser sprites.</param>
/// <param name="Variant">The preferred variant for kits, or the source variant for browser sprites.</param>
/// <param name="File">The file path relative to this server's icon cache folder.</param>
public sealed record IconCacheArtifact(string Kind, string? Kit, string? Library, string? Variant, string File);
