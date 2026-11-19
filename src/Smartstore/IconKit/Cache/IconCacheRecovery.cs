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
    public IReadOnlyCollection<IconCacheArtifact> GetCurrentFiles(
        IconCatalog catalog,
        IReadOnlySet<string> files,
        IReadOnlyCollection<IconCacheArtifact> knownFiles = null)
    {
        if (files.Count == 0)
        {
            return [];
        }

        if (knownFiles == null || knownFiles.Count == 0)
        {
            return FindUnindexedFiles(catalog, files);
        }

        var result = new HashSet<IconCacheArtifact>();
        var unknownFiles = files.ToHashSet(StringComparer.Ordinal);

        foreach (var known in knownFiles)
        {
            unknownFiles.Remove(known.File);
            var current = GetCurrentFile(catalog, known);
            if (current != null && files.Contains(current.File))
            {
                result.Add(current);
                unknownFiles.Remove(current.File);
            }
        }

        // A filename alone does not identify a kit's preferred library and variant.
        // This also rechecks old files that may be current again after a source rollback.
        if (unknownFiles.Count > 0)
        {
            result.UnionWith(FindUnindexedFiles(catalog, unknownFiles));
        }

        return result.ToArray();
    }

    private IconCacheArtifact GetCurrentFile(IconCatalog catalog, IconCacheArtifact known)
    {
        if (known.Kind == "manifest")
        {
            return known with { File = "kits/manifest-" + _kitPlanner.GetManifest(catalog).Revision + ".json" };
        }

        // Removed libraries or variants must not silently fall back to another selection.
        var library = catalog.SelectLibrary(known.Library);
        if (library == null || known.Variant == null || !library.Variants.ContainsKey(known.Variant))
        {
            return null;
        }

        if (known.Kind == "browser" && !library.IsProvider)
        {
            var revision = _librarySprites.GetRevision(catalog, known.Library, known.Variant);
            return known with { File = $"browser/{known.Library}-{known.Variant}-{revision}.svg" };
        }

        if (known.Kind == "kit" && known.Kit != null && catalog.Kits.ContainsKey(known.Kit))
        {
            var index = _kitPlanner.GetIndex(catalog, known.Library, known.Variant);
            if (index.CanGenerate(known.Kit))
            {
                return known with { File = $"kits/{known.Kit}-{index.Plans[known.Kit].Value.Revision}.svg" };
            }
        }

        return null;
    }

    private IReadOnlyCollection<IconCacheArtifact> FindUnindexedFiles(IconCatalog catalog, IReadOnlySet<string> files)
    {
        var result = new List<IconCacheArtifact>();
        if (files.Count == 0)
        {
            return result;
        }

        var spritePrefixes = new HashSet<string>(StringComparer.Ordinal);
        bool hasManifest = false;

        foreach (var file in files)
        {
            hasManifest |= file.StartsWith("kits/manifest-", StringComparison.Ordinal);
            if (file.EndsWith(".svg", StringComparison.Ordinal))
            {
                // The last hyphen separates the revision; names may contain hyphens too.
                spritePrefixes.Add(file[..(file.LastIndexOf('-') + 1)]);
            }
        }

        // A kit can have several valid preferences at once. Reconstruct all supported selections
        // for kits present on disk so one request does not retire another selection's sprite.
        var kitNames = catalog.Kits.Keys
            .Where(name => spritePrefixes.Contains("kits/" + name + "-")).ToArray();

        foreach (var library in catalog.Libraries.Values.Distinct())
        {
            foreach (var variant in library.Variants.Values.Distinct())
            {
                // Browser sprites have exactly one source variant and do not use kit mappings.
                var browserPrefix = "browser/" + library.Manifest.SystemName + "-" + variant.Manifest.Name + "-";
                if (!library.IsProvider && spritePrefixes.Contains(browserPrefix))
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
        if (hasManifest)
        {
            var manifest = _kitPlanner.GetManifest(catalog);
            result.Add(new("manifest", null, null, null, "kits/manifest-" + manifest.Revision + ".json"));
        }

        return result.Where(x => files.Contains(x.File)).ToArray();
    }

}
