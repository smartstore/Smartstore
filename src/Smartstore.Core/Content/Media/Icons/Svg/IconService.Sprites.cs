namespace Smartstore.Core.Content.Media.Icons;

public sealed partial class IconService
{
    /// <summary>
    /// Generates the complete selected variant once and returns its immutable revision.
    /// </summary>
    /// <param name="catalog">The source generation retained for this operation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    internal async Task<string> PrepareSpriteAsync(IconCatalog catalog, IconCatalog.Library library,
        IconCatalog.Variant variant, CancellationToken cancelToken)
    {
        var sprite = GetSprite(catalog, library, variant);
        await sprite.PublishAsync(GetSpritePath(library, variant, sprite.Revision), cancelToken);
        return sprite.Revision;
    }

    /// <summary>
    /// Retrieves a historical variant sprite or generates a missing file for the current revision.
    /// </summary>
    /// <param name="catalog">The source generation retained for this operation.</param>
    /// <param name="library">The registered library.</param>
    /// <param name="variant">The registered variant.</param>
    /// <param name="revision">The requested content fingerprint.</param>
    internal Task<string> GetSpriteFileAsync(IconCatalog catalog, IconCatalog.Library library,
        IconCatalog.Variant variant, string revision, CancellationToken cancelToken)
    {
        if (!IconFileCache.IsRevision(revision))
        {
            return Task.FromResult<string>(null);
        }

        var path = GetSpritePath(library, variant, revision);
        if (File.Exists(path))
        {
            return Task.FromResult(path);
        }

        var sprite = GetSprite(catalog, library, variant);
        return sprite.Revision == revision ? sprite.PublishAsync(path, cancelToken) : Task.FromResult<string>(null);
    }

    /// <summary>
    /// Defers complete variant plans until first use, bypassing conceptual mappings.
    /// </summary>
    /// <param name="catalog">The owning source generation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    private static IconSprite GetSprite(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => catalog.VariantSprites.GetOrAdd(library.Manifest.SystemName + ":" + variant.Manifest.Name,
            _ => new Lazy<IconSprite>(() => new IconSprite(catalog, library, variant, "browser",
                variant.Names.OrderBy(x => x, StringComparer.Ordinal)
                    .Select(name => (name, CreateInfo(library, variant, name, false), variant))))).Value;

    /// <summary>
    /// Builds a cache path from registered identities and a validated revision.
    /// </summary>
    /// <param name="library">The registered library.</param>
    /// <param name="variant">The registered variant.</param>
    /// <param name="revision">The content fingerprint.</param>
    private string GetSpritePath(IconCatalog.Library library, IconCatalog.Variant variant, string revision)
        => Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "icons", "browser",
            library.Manifest.SystemName + "-" + variant.Manifest.Name + "-" + revision + ".svg");
}