using Smartstore.IconKit.Sources;

namespace Smartstore.IconKit.Sprites;

/// <summary>
/// Indexes conceptual memberships for one library/variant and defers source revisions per kit.
/// Owned by the catalog generation; contains no live files or SVG bodies.
/// </summary>
public sealed class IconKitIndex
{
    // A kit can remain usable even when some mapped sources are missing.
    // Check once per generation, not once per rendered icon.
    private readonly Dictionary<string, Lazy<bool>> _availability = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the preferred kit and symbol for each actual icon and mapping transformation.
    /// </summary>
    internal Dictionary<(string Area, string Concept, string Address, IconTransform Transform, double StrokeScale), (string Kit, string Symbol, bool Pinned)> Memberships { get; } = new();

    /// <summary>
    /// Gets deferred source plans keyed by configured kit name.
    /// </summary>
    internal Dictionary<string, Lazy<IconSprite>> Plans { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets resolved source selections without opening SVG bodies or evaluating sprite plans.
    /// </summary>
    internal Dictionary<string, Entry[]> Entries { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Maps concepts without parsing SVG bodies. Kit declaration order determines precedence.
    /// </summary>
    /// <param name="catalog">The owning source generation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    internal IconKitIndex(
        IconCatalog catalog,
        IconCatalog.Library library,
        IconCatalog.Variant variant,
        IIconResolver resolver)
    {
        foreach (var kit in catalog.Kits.Values)
        {
            var entries = kit.Icons.OrderBy(x => x, StringComparer.Ordinal).Select(concept =>
            {
                // Source overrides are concrete and stay fixed when the kit is requested
                // with alternate defaults. Ordinary concepts still use the selected mapping.
                var address = kit.SourceAddresses.GetValueOrDefault(concept);
                var pinned = !address.IsEmpty;
                var entryLibrary = pinned ? catalog.SelectLibrary(address.Library ?? kit.DefaultLibrary) : library;
                var entryVariant = pinned ? catalog.SelectVariant(entryLibrary, address.Variant, kit) : variant;
                var resolved = resolver.Resolve(catalog, new IconAddress(pinned ? address.Name : concept,
                    entryLibrary.Manifest.SystemName, entryVariant.Manifest.Name, pinned), indexOnly: true);
                if (resolved is { } source)
                {
                    var presentation = pinned ? kit.SourcePresentations.GetValueOrDefault(concept) : default;
                    var sourceMapping = new IconMapping(source.Name, source.Transform, source.StrokeScale) with
                    {
                        Transform = presentation.Apply(source.Transform),
                        StrokeScale = presentation.StrokeScale ?? source.StrokeScale
                    };
                    var sourceLibrary = catalog.SelectLibrary(source.LibraryName);
                    var sourceVariant = catalog.SelectVariant(sourceLibrary, source.VariantName);
                    return new Entry(concept, sourceLibrary, sourceVariant, sourceMapping, pinned);
                }

                // Retain missing entries for resolution; sprite and browser output skip them.
                var mapping = pinned ? new IconMapping(address.Name, default)
                    : entryLibrary.Mapping.GetValueOrDefault(concept) ?? new IconMapping(concept, default);
                return new Entry(concept, entryLibrary, entryVariant, mapping, pinned);
            }).ToArray();
            Entries.Add(kit.Name, entries);
            _availability.Add(kit.Name, new Lazy<bool>(() => entries.Any(x => x.Variant.Names.Contains(x.Mapping.Name))));
            foreach (var entry in entries)
            {
                // Mixed kits can contain identical names from different libraries or variants.
                // Match the actual canonical identity, not just the unqualified source name.
                var address = entry.CreateInfo().Address;
                foreach (var area in kit.Areas)
                {
                    var member = (kit.Name, entry.Concept, entry.Pinned);
                    Memberships.TryAdd((area, null, address, entry.Mapping.Transform, entry.Mapping.StrokeScale), member);
                    if (catalog.FindKit(entry.Concept, area) == kit)
                    {
                        Memberships.TryAdd((area, entry.Concept, address, entry.Mapping.Transform, entry.Mapping.StrokeScale), member);
                    }
                }
            }

            Plans.Add(kit.Name, new Lazy<IconSprite>(() =>
            {
                var sprite = new IconSprite(catalog, library, variant, kit.Name,
                    entries.Select(entry => (entry.Concept, entry.CreateInfo(), entry.Variant)));
                catalog.KitRevisions.TryAdd(kit.Name + ":" + sprite.Revision, sprite);
                return sprite;
            }));
        }
    }

    /// <summary>
    /// Identifies the source of a public symbol before any drawing data is read.
    /// </summary>
    /// <param name="Concept">The unqualified name used as the public symbol ID.</param>
    /// <param name="Library">The library supplying this symbol.</param>
    /// <param name="Variant">The variant supplying this symbol.</param>
    /// <param name="Mapping">The actual source name and its mapping modifiers.</param>
    /// <param name="Pinned">Whether a source override fixes this symbol's selection.</param>
    internal sealed record Entry(string Concept, IconCatalog.Library Library, IconCatalog.Variant Variant, IconMapping Mapping, bool Pinned)
    {
        /// <summary>
        /// Creates metadata using this kit's source and modifiers, independent of global concept precedence.
        /// </summary>
        internal IconInfo CreateInfo()
        {
            var info = IconInfo.Create(Library, Variant, Mapping.Name, false);
            info.Transform = Mapping.Transform;
            info.StrokeScale = Mapping.StrokeScale;
            return info;
        }
    }

    /// <summary>
    /// Checks source availability across every library used by one kit, without parsing SVGs.
    /// </summary>
    /// <param name="kit">The configured kit name.</param>
    internal bool CanGenerate(string kit) => _availability[kit].Value;
}
