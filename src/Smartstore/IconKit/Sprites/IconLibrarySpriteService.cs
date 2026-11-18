using Smartstore.IconKit.Sources;
using Microsoft.Extensions.Options;

namespace Smartstore.IconKit.Sprites;

public sealed class IconLibrarySpriteService : IIconLibrarySpriteService
{
    private readonly IconKitOptions _options;

    public IconLibrarySpriteService(IOptions<IconKitOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public string GetRevision(IconCatalog catalog, string libraryName, string variantName)
    {
        var library = catalog.SelectLibrary(libraryName);
        var variant = catalog.SelectVariant(library, variantName);
        return variant == null ? null : GetSprite(catalog, library, variant).Revision;
    }

    /// <inheritdoc />
    public async Task<string> PrepareAsync(IconCatalog catalog, string libraryName, string variantName, CancellationToken cancelToken = default)
    {
        var library = catalog.SelectLibrary(libraryName);
        var variant = catalog.SelectVariant(library, variantName);
        if (variant == null)
        {
            return null;
        }

        var sprite = GetSprite(catalog, library, variant);
        await sprite.PublishAsync(GetSpritePath(library, variant, sprite.Revision), cancelToken);
        return sprite.Revision;
    }

    // Defers complete variant plans until first use, bypassing conceptual mappings.
    private IconSprite GetSprite(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => catalog.VariantSprites.GetOrAdd(library.Manifest.SystemName + ":" + variant.Manifest.Name,
            _ => new Lazy<IconSprite>(() => new IconSprite(catalog, library, variant, "browser",
                variant.Names.OrderBy(x => x, StringComparer.Ordinal)
                    .Select(name => (name, IconInfo.Create(library, variant, name, false), variant))))).Value;

    private string GetSpritePath(IconCatalog.Library library, IconCatalog.Variant variant, string revision)
        => Path.Combine(_options.CachePath, "browser",
            library.Manifest.SystemName + "-" + variant.Manifest.Name + "-" + revision + ".svg");
}
