namespace Smartstore.IconKit.Sources;

public sealed class IconResolver : IIconResolver
{
    /// <inheritdoc />
    public IconInfo Resolve(IconCatalog catalog, IconAddress address, bool indexOnly = false)
    {
        Guard.NotNull(catalog);

        var library = catalog.SelectLibrary(address.Library);
        var variant = catalog.SelectVariant(library, address.Variant);
        if (variant == null)
        {
            return null;
        }

        var name = address.Name;
        var skipMapping = address.SkipMapping;
        var result = FindInLibrary(library, variant);
        if (result == null && !skipMapping && catalog.FallbackToDefaultLibrary && library != catalog.DefaultLibrary)
        {
            // Do not carry a foreign concrete name, mapping modifiers, or kit variant across
            // libraries. The system default resolves the original concept independently.
            result = FindInLibrary(catalog.DefaultLibrary, catalog.SelectVariant(catalog.DefaultLibrary, null));
        }

        if (result is not { } source)
        {
            return null;
        }

        var info = IconInfo.Create(source.Library, source.Variant, source.Mapping.Name, includeTags: !indexOnly);
        info.Transform = source.Mapping.Transform;
        info.StrokeScale = source.Mapping.StrokeScale;
        return info;

        (IconCatalog.Library Library, IconCatalog.Variant Variant, IconMapping Mapping)? FindInLibrary(
            IconCatalog.Library candidateLibrary, IconCatalog.Variant candidateVariant)
        {
            var mapping = (!skipMapping ? candidateLibrary.Mapping.GetValueOrDefault(name) : null)
                ?? new IconMapping(name, default);
            foreach (var candidate in candidateVariant.ResolutionOrder)
            {
                // Individual lookups retain the direct override fast path: no directory scan
                // or archive access when a loose override exists. Kits use name indexes so
                // resolving memberships does not fingerprint unrelated kits' loose sources.
                if (indexOnly ? candidate.Names.Contains(mapping.Name) : candidate.GetSource(mapping.Name) != null)
                {
                    return (candidateLibrary, candidate, candidate.Manifest.SupportsStrokeScaling ? mapping : mapping with { StrokeScale = 1 });
                }
            }

            return null;
        }
    }
}
