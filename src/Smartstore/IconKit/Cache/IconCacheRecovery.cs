using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;

namespace Smartstore.IconKit.Cache;

public sealed class IconCacheRecovery : IIconCacheRecovery
{
    private readonly IIconSpritePlanner _planner;

    public IconCacheRecovery(IIconSpritePlanner planner)
    {
        _planner = planner;
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
            var current = GetCurrentFile(catalog, known.Kind, known.Library, known.Variant, known.Kit);
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

    private IconCacheArtifact GetCurrentFile(
        IconCatalog catalog,
        string kind,
        string libraryName = null,
        string variantName = null,
        string kitName = null)
    {
        if (kind == "manifest")
        {
            return new(kind, kitName, libraryName, variantName,
                "kits/manifest-" + _planner.GetManifest(catalog).Revision + ".json");
        }

        // Removed sources must not fall back to another library or variant.
        var library = catalog.SelectLibrary(libraryName);
        if (library == null || variantName == null || !library.Variants.ContainsKey(variantName))
        {
            return null;
        }

        if (kind == "browser" && !library.IsProvider)
        {
            var revision = _planner.GetLibrarySprite(catalog, libraryName, variantName).Revision;
            return new(kind, kitName, libraryName, variantName, $"browser/{libraryName}-{variantName}-{revision}.svg");
        }

        if (kind == "kit" && kitName != null && catalog.Kits.ContainsKey(kitName))
        {
            var index = _planner.GetIndex(catalog, libraryName, variantName);
            if (index.CanGenerate(kitName))
            {
                return new(kind, kitName, libraryName, variantName, $"kits/{kitName}-{index.Plans[kitName].Value.Revision}.svg");
            }
        }

        return null;
    }

    private IReadOnlyCollection<IconCacheArtifact> FindUnindexedFiles(IconCatalog catalog, IReadOnlySet<string> files)
    {
        var result = new List<IconCacheArtifact>();
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
                    result.Add(GetCurrentFile(catalog, "browser", library.Manifest.SystemName, variant.Manifest.Name));
                }

                foreach (var name in kitNames)
                {
                    var current = GetCurrentFile(catalog, "kit", library.Manifest.SystemName, variant.Manifest.Name, name);
                    if (current != null)
                    {
                        result.Add(current);
                    }
                }
            }
        }

        // The browser's manifest is another immutable artifact with its own revision.
        if (hasManifest)
        {
            result.Add(GetCurrentFile(catalog, "manifest"));
        }

        return result.Where(x => files.Contains(x.File)).ToArray();
    }

}
