using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.IconKit.Sources;
using Smartstore.IO;

namespace Smartstore.IconKit.Sprites;

public sealed class IconLibrarySpriteService : IIconLibrarySpriteService
{
    private readonly IFileSystem _fileCache;

    public IconLibrarySpriteService(IApplicationContext appContext, IOptions<IconKitOptions> options)
    {
        _fileCache = IconFileHelper.CreateFileSystem(appContext, options.Value.CachePath);
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
        var file = _fileCache.GetFile("browser/" + library.Manifest.SystemName + "-" + variant.Manifest.Name + "-" + sprite.Revision + ".svg");
        await sprite.PublishAsync(file, cancelToken);
        return sprite.Revision;
    }

    // Defers complete variant plans until first use, bypassing conceptual mappings.
    private IconSprite GetSprite(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => catalog.VariantSprites.GetOrAdd(library.Manifest.SystemName + ":" + variant.Manifest.Name,
            _ => new Lazy<IconSprite>(() => new IconSprite(catalog, library, variant, "browser",
                variant.Names.OrderBy(x => x, StringComparer.Ordinal)
                    .Select(name => (name, IconInfo.Create(library, variant, name, false), variant))))).Value;

}
