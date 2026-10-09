using System.Security.Cryptography;
using System.Text.Json;
using Smartstore.Json;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Resolves external sprite URLs and publishes immutable sprite files in App_Data/.cache/icons/kits.
/// </summary>
/// <param name="icons">Provides the shared source catalog.</param>
/// <param name="applicationContext">Provides the physical application data root.</param>
/// <param name="httpContextAccessor">Supplies the application's request path base.</param>
public sealed class IconKitService(IconService icons, IApplicationContext applicationContext, IHttpContextAccessor httpContextAccessor) : IIconKitService
{
    /// <inheritdoc />
    public string GetManifestUrl()
        => (httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + "/icons/manifest/" + GetManifest(icons.Catalog).Revision + ".json";

    /// <inheritdoc />
    public async Task<string> GetManifestFileAsync(string revision, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        if (!IconFileCache.IsRevision(revision))
        {
            return null;
        }

        var directory = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "icons", "kits");
        var path = Path.Combine(directory, "manifest-" + revision + ".json");
        if (File.Exists(path))
        {
            return path;
        }

        var catalog = icons.Catalog;
        var manifest = GetManifest(catalog);
        if (manifest.Revision != revision)
        {
            return null;
        }

        return await IconFileCache.PublishAsync(path, async (stream, token) =>
        {
            await stream.WriteAsync(manifest.Content, token);
            if (catalog.ChangeToken.HasChanged)
            {
                throw new IOException("Icon sources changed during manifest creation. Retry after catalog reload.");
            }
        }, cancelToken);
    }

    private static (string Revision, byte[] Content) GetManifest(IconCatalog catalog)
    {
        var lazy = Volatile.Read(ref catalog.BrowserManifest);
        if (lazy == null)
        {
            lazy = new Lazy<(string, byte[])>(() => CreateManifest(catalog));
            lazy = Interlocked.CompareExchange(ref catalog.BrowserManifest, lazy, null) ?? lazy;
        }

        return lazy.Value;
    }

    /// <summary>
    /// Publishes directly renderable concepts, kit URLs and compact DOM identities.
    /// Source resolution stays server-side; identity patches never change symbol references.
    /// </summary>
    /// <param name="catalog">The immutable source generation supplying mappings and kit plans.</param>
    private static (string Revision, byte[] Content) CreateManifest(IconCatalog catalog)
    {
        var kits = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var kit in catalog.Kits.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            var library = IconService.SelectLibrary(catalog, kit.DefaultLibrary);
            var variant = IconService.SelectVariant(catalog, library, null, kit);
            var index = GetIndex(catalog, library, variant);
            // Every concept has its own symbol, even when several concepts share artwork.
            // Mapping transforms are baked into symbols; stroke multipliers require inline SVG.
            var entries = index.Entries[kit.Name]
                .Where(x => catalog.ConceptKits[x.Concept] == kit && x.Mapping.StrokeScale == 1)
                .ToArray();
            if (entries.Length == 0 || !index.CanGenerate(kit.Name))
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
                        variantKey != defaultVariant ? "@" + variantKey : string.Empty));
                }
            }

            // Fingerprint source descriptors only. Manifest requests neither generate sprites
            // nor read SVG drawings into the individual icon cache.
            kits.Add(kit.Name, new
            {
                Url = "icons/" + Uri.EscapeDataString(kit.Name) + "-" + index.Plans[kit.Name].Value.Revision + ".svg",
                DefaultLibrary = defaultLibrary,
                DefaultVariant = defaultVariant,
                Icons = entries.Select(x => x.Concept).ToArray(),
                Sources = sources
            });
        }

        var content = JsonSerializer.SerializeToUtf8Bytes(new
        {
            SchemaVersion = 6,
            Kits = kits
        }, SmartJsonOptions.CamelCased);
        return (Convert.ToHexStringLower(SHA256.HashData(content).AsSpan(0, 12)), content);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IconKit> Kits => icons.Catalog.Kits.Values;

    /// <inheritdoc />
    public IconKitReference GetReference(IconInfo icon)
    {
        Guard.NotNull(icon);

        var catalog = icons.Catalog;
        var index = GetIndex(catalog, icon.SelectionLibraryName ?? icon.LibraryName, icon.SelectionVariantName ?? icon.VariantName);
        if (index == null || !index.Memberships.TryGetValue((icon.Address, icon.Transform), out var member))
        {
            return null;
        }

        // A pinned BI member of an FA kit must reference that kit's default mixed sprite,
        // not try to regenerate all of its ordinary FA concepts with BI defaults.
        if (member.Pinned)
        {
            var kit = catalog.Kits[member.Kit];
            var library = IconService.SelectLibrary(catalog, kit.DefaultLibrary);
            var variant = IconService.SelectVariant(catalog, library, null, kit);
            index = GetIndex(catalog, library, variant);
        }

        if (!index.CanGenerate(member.Kit))
        {
            // Explicit selections can match one symbol without being able to supply
            // the whole kit. Let the renderer use the individual source in that case.
            return null;
        }

        var plan = index.Plans[member.Kit].Value;
        return new IconKitReference(BuildUrl(member.Kit, plan.Revision)
            + "#" + Uri.EscapeDataString(member.Symbol));
    }

    /// <inheritdoc />
    public string GetUrl(string kitName, string library = null, string variant = null)
    {
        var catalog = icons.Catalog;
        if (kitName == null || !catalog.Kits.TryGetValue(kitName, out var kit))
        {
            return null;
        }

        var selectedLibrary = IconService.SelectLibrary(catalog, library ?? kit.DefaultLibrary);
        var selectedVariant = IconService.SelectVariant(catalog, selectedLibrary, variant, kit);
        if (selectedVariant == null)
        {
            return null;
        }

        var index = GetIndex(catalog, selectedLibrary, selectedVariant);
        return BuildUrl(kitName, index.Plans[kitName].Value.Revision);
    }

    /// <inheritdoc />
    public async Task<string> GetSpriteFileAsync(string kitName, string revision, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        // Only revision hashes may enter physical filenames; arbitrary request input must
        // neither escape the cache directory nor trigger generation for invented revisions.
        if (!IconAddress.IsQualifier(kitName) || !IconFileCache.IsRevision(revision))
        {
            return null;
        }

        var directory = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "icons", "kits");
        var path = Path.Combine(directory, $"{kitName}-{revision}.svg");
        // Retain historical revisions for already rendered pages, even after sources change.
        // Nothing is read into memory and no source plan is needed for an existing file.
        if (File.Exists(path))
        {
            return path;
        }

        var catalog = icons.Catalog;
        if (!catalog.Kits.ContainsKey(kitName))
        {
            return null;
        }

        var key = kitName + ":" + revision;
        if (!catalog.KitRevisions.TryGetValue(key, out var plan))
        {
            // Normally URL generation has already registered the plan. After a restart,
            // a missing file can still be reconstructed using current manifest identities.
            // Recovery is bounded by configured kits, never by arbitrary revision strings.
            _ = catalog.RecoveredKits.GetOrAdd(kitName,
                name => new Lazy<bool>(() => RecoverPlans(catalog, name))).Value;
            if (!catalog.KitRevisions.TryGetValue(key, out plan))
            {
                return null;
            }
        }

        return await plan.PublishAsync(path, cancelToken);
    }

    /// <summary>
    /// Prepares a kit and searches its own resolved concepts, source names and tags.
    /// </summary>
    /// <param name="catalog">The source generation retained for the entire operation.</param>
    /// <param name="kitName">The configured kit name.</param>
    /// <param name="query">The validated search and pagination options.</param>
    internal async Task<(string Url, int TotalCount, (string Concept, IconInfo Info)[] Items)?> SearchAsync(
        IconCatalog catalog, string kitName, IconSearchQuery query, CancellationToken cancelToken)
    {
        if (!catalog.Kits.TryGetValue(kitName, out var kit))
        {
            return null;
        }

        var library = IconService.SelectLibrary(catalog, kit.DefaultLibrary);
        var variant = IconService.SelectVariant(catalog, library, null, kit);
        var index = GetIndex(catalog, library, variant);
        var sprite = index.Plans[kitName].Value;
        var path = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "icons", "kits", kitName + "-" + sprite.Revision + ".svg");
        await sprite.PublishAsync(path, cancelToken);

        // Search the selected kit, not global concept precedence. Empty searches load no tags.
        var page = IconService.FindPage(query, index.Entries[kitName], (entry, word) =>
            entry.Concept.Contains(word, StringComparison.OrdinalIgnoreCase)
            || entry.Mapping.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || (entry.Library.Tags.GetValueOrDefault(entry.Mapping.Name) ?? [])
                .Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase)));
        return (BuildUrl(kitName, sprite.Revision), page.TotalCount,
            page.Items.Select(entry => (entry.Concept, entry.CreateInfo())).ToArray());
    }

    /// <summary>
    /// Reconstructs opaque revision lookups from current configuration after a cold endpoint request.
    /// Does not enumerate the cache directory or parse SVG drawings. Incomplete library variants
    /// cannot supply this kit and are skipped; normal URL generation still reports missing targets.
    /// </summary>
    /// <param name="catalog">The current source generation.</param>
    /// <param name="kit">The configured kit to recover.</param>
    private static bool RecoverPlans(IconCatalog catalog, string kit)
    {
        foreach (var library in catalog.Libraries.Values.Distinct())
        {
            foreach (var variant in library.Variants.Values.Distinct())
            {
                var index = GetIndex(catalog, library, variant);
                if (index.CanGenerate(kit))
                {
                    // Evaluating a plan registers its fingerprint in the owning catalog.
                    _ = index.Plans[kit].Value;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Selects only registered manifests, bounding index entries to configured variants.
    /// </summary>
    /// <param name="catalog">The source generation retained for the entire lookup.</param>
    /// <param name="library">The library selector.</param>
    /// <param name="variant">The variant selector.</param>
    private static IconKitIndex GetIndex(IconCatalog catalog, string library, string variant)
    {
        var selectedLibrary = IconService.SelectLibrary(catalog, library);
        var selectedVariant = IconService.SelectVariant(catalog, selectedLibrary, variant);
        return selectedVariant == null ? null : GetIndex(catalog, selectedLibrary, selectedVariant);
    }

    /// <summary>
    /// Reuses one deferred membership index per variant and source generation.
    /// </summary>
    /// <param name="catalog">The current source generation.</param>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    internal static IconKitIndex GetIndex(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => catalog.KitIndexes.GetOrAdd(library.Manifest.SystemName + ":" + variant.Manifest.Name,
            _ => new Lazy<IconKitIndex>(() => new IconKitIndex(catalog, library, variant))).Value;

    /// <summary>
    /// Builds the endpoint path, preserving application hosting beneath a path base.
    /// </summary>
    /// <param name="kit">The configured kit name.</param>
    /// <param name="revision">The output fingerprint.</param>
    private string BuildUrl(string kit, string revision)
        => (httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + "/icons/" + Uri.EscapeDataString(kit) + "-" + revision + ".svg";
}
