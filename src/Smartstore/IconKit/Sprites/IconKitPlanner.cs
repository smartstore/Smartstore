using Smartstore.IconKit.Sources;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Smartstore.Json;

namespace Smartstore.IconKit.Sprites;

public sealed class IconKitPlanner : IIconKitPlanner
{
    private readonly IIconResolver _resolver;
    private readonly IconKitOptions _options;

    public IconKitPlanner(IIconResolver resolver, IOptions<IconKitOptions> options)
    {
        _resolver = resolver;
        _options = options.Value;
    }

    /// <inheritdoc />
    public IconKitManifest GetManifest(IconCatalog catalog)
    {
        var lazy = Volatile.Read(ref catalog.BrowserManifest);
        if (lazy == null)
        {
            lazy = new Lazy<IconKitManifest>(() => CreateManifest(catalog));
            lazy = Interlocked.CompareExchange(ref catalog.BrowserManifest, lazy, null) ?? lazy;
        }

        return lazy.Value;
    }

    // Publishes directly renderable concepts, kit URLs and compact DOM identities.
    // Source resolution stays server-side; identity patches never change symbol references.
    private IconKitManifest CreateManifest(IconCatalog catalog)
    {
        var kits = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kit in catalog.Kits.Values)
        {
            var library = catalog.SelectLibrary(kit.DefaultLibrary);
            var variant = catalog.SelectVariant(library, null, kit);
            var index = GetIndex(catalog, library, variant);
            // Every concept has its own symbol, even when several concepts share artwork.
            // Mapping transforms and stroke multipliers are baked into symbols.
            var entries = index.GetAvailableEntries(kit.Name);
            if (entries.Length == 0)
            {
                // Do not substitute a lower-priority kit for an unavailable preferred kit.
                // The render endpoint still resolves individual concepts with their defaults.
                continue;
            }

            var defaultLibrary = library.Manifest.ShortName ?? library.Manifest.SystemName;
            var defaultVariant = variant.Manifest.ShortName ?? variant.Manifest.Name;
            var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                var libraryKey = entry.Library.Manifest.ShortName ?? entry.Library.Manifest.SystemName;
                var variantKey = entry.Variant.Manifest.ShortName ?? entry.Variant.Manifest.Name;
                if (entry.Mapping.Name != entry.Concept || libraryKey != defaultLibrary || variantKey != defaultVariant)
                {
                    // These are final identities, not mappings for the client to resolve.
                    // Missing qualifiers inherit this kit's defaults, even across libraries.
                    sources.Add(entry.Concept, string.Concat(
                        libraryKey != defaultLibrary ? libraryKey + ":" : string.Empty,
                        entry.Mapping.Name,
                        variantKey != defaultVariant ? "/" + variantKey : string.Empty));
                }
            }

            // Fingerprint source descriptors only. Manifest requests neither generate sprites
            // nor read SVG drawings into the individual icon cache.
            var manifestKit = new Dictionary<string, object>
            {
                ["areas"] = kit.Areas,
                ["url"] = _options.RoutePrefix.TrimStart('/') + "/" + Uri.EscapeDataString(kit.Name) + "-" + index.Plans[kit.Name].Value.Revision + ".svg",
                ["defaultLibrary"] = defaultLibrary,
                ["defaultVariant"] = defaultVariant,
                ["icons"] = entries.Select(x => x.Concept).ToArray(),
                ["duotone"] = entries.Where(x => x.Variant.Manifest.IsDuotone).Select(x => x.Concept).ToArray(),
                ["supportsStrokeScaling"] = entries.Where(x => x.Variant.Manifest.SupportsStrokeScaling).Select(x => x.Concept).ToArray(),
                ["sources"] = sources,
                ["transforms"] = entries.Where(x => !x.Mapping.Transform.IsIdentity).ToDictionary(x => x.Concept,
                    x => FormattableString.Invariant($"{(x.Mapping.Transform.FlipX ? 1 : 0)} {(x.Mapping.Transform.FlipY ? 1 : 0)} {x.Mapping.Transform.Rotation:R}"))
            };
            var external = catalog.ExternalIcons[kit.Name];
            if (external.Count > 0)
            {
                // Zero-based positions in this kit's sorted icons array; never repeat concept names.
                manifestKit.Add("external", entries.Select((entry, i) => (entry, i))
                    .Where(x => external.Contains(x.entry.Concept)).Select(x => x.i).ToArray());
            }
            kits.Add(kit.Name, manifestKit);
        }

        var content = JsonSerializer.SerializeToUtf8Bytes(new
        {
            SchemaVersion = 12,
            MirrorInRtl = catalog.MirrorInRtl.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            Areas = catalog.ConceptKits.GroupBy(x => x.Key.Area).ToDictionary(
                group => group.Key, group => group.ToDictionary(x => x.Key.Concept, x => x.Value.Name)),
            Kits = kits
        }, SmartJsonOptions.CamelCased);
        return new IconKitManifest(Convert.ToHexStringLower(SHA256.HashData(content).AsSpan(0, 12)), content);
    }

    /// <inheritdoc />
    public IconKitIndex GetIndex(IconCatalog catalog, string library, string variant)
    {
        var selectedLibrary = catalog.SelectLibrary(library);
        var selectedVariant = catalog.SelectVariant(selectedLibrary, variant);
        return selectedVariant == null ? null : GetIndex(catalog, selectedLibrary, selectedVariant);
    }

    private IconKitIndex GetIndex(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => catalog.KitIndexes.GetOrAdd(library.Manifest.SystemName + ":" + variant.Manifest.Name,
            static (_, state) => new Lazy<IconKitIndex>(() =>
                new IconKitIndex(state.Catalog, state.Library, state.Variant, state.Resolver)),
            (Catalog: catalog, Library: library, Variant: variant, Resolver: _resolver)).Value;

}
