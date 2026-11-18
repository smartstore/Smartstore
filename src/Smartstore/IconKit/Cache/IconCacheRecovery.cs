using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;

namespace Smartstore.IconKit.Cache;

public sealed class IconCacheRecovery : IIconCacheRecovery
{
    private readonly IIconKitPlanner _kitPlanner;
    private readonly IIconLibrarySpriteService _librarySprites;

    public IconCacheRecovery(IIconKitPlanner kitPlanner, IIconLibrarySpriteService librarySprites)
    {
        _kitPlanner = kitPlanner;
        _librarySprites = librarySprites;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IconCacheArtifact> GetCurrentFiles(IconCatalog catalog, IReadOnlySet<string> files)
    {
        var result = new List<IconCacheArtifact>();
        if (files.Count == 0)
        {
            return result;
        }

        // A kit can have several valid preferences at once. Reconstruct all supported selections
        // for kits present on disk so one request does not retire another selection's sprite.
        var kitNames = catalog.Kits.Keys.Where(name => files.Any(file =>
            file.StartsWith("kits/" + name + "-", StringComparison.Ordinal)
            && file.EndsWith(".svg", StringComparison.Ordinal))).ToArray();

        foreach (var library in catalog.Libraries.Values.Distinct())
        {
            foreach (var variant in library.Variants.Values.Distinct())
            {
                // Browser sprites have exactly one source variant and do not use kit mappings.
                var browserPrefix = "browser/" + library.Manifest.SystemName + "-" + variant.Manifest.Name + "-";
                if (!library.IsProvider && files.Any(file => file.StartsWith(browserPrefix, StringComparison.Ordinal)))
                {
                    var revision = _librarySprites.GetRevision(catalog, library.Manifest.SystemName, variant.Manifest.Name);
                    result.Add(new("browser", null, library.Manifest.SystemName, variant.Manifest.Name,
                        browserPrefix + revision + ".svg"));
                }

                if (kitNames.Length == 0)
                {
                    continue;
                }

                // Mixed kits resolve their mappings and fallback variants through the normal index.
                var index = _kitPlanner.GetIndex(catalog, library.Manifest.SystemName, variant.Manifest.Name);
                foreach (var name in kitNames)
                {
                    if (index.CanGenerate(name))
                    {
                        var sprite = index.Plans[name].Value;
                        result.Add(new("kit", name, library.Manifest.SystemName, variant.Manifest.Name,
                            "kits/" + name + "-" + sprite.Revision + ".svg"));
                    }
                }
            }
        }

        // The browser's manifest is another immutable artifact with its own revision.
        if (files.Any(file => file.StartsWith("kits/manifest-", StringComparison.Ordinal)))
        {
            var manifest = _kitPlanner.GetManifest(catalog);
            result.Add(new("manifest", null, null, null, "kits/manifest-" + manifest.Revision + ".json"));
        }

        return result.Where(x => files.Contains(x.File)).ToArray();
    }

}
