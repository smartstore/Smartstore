using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace Smartstore.IconKit;

public sealed class IconKitService : IIconKitService
{
    private readonly IIconCatalogAccessor _catalogAccessor;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IIconKitPlanner _planner;
    private readonly IconKitOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IconKitService(
        IIconCatalogAccessor catalogAccessor,
        IIconCacheLifecycle lifecycle,
        IIconKitPlanner planner,
        IOptions<IconKitOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _catalogAccessor = catalogAccessor;
        _lifecycle = lifecycle;
        _planner = planner;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string GetManifestUrl()
        => (_httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + _options.RoutePrefix + "/manifest/" + _planner.GetManifest(_catalogAccessor.Catalog).Revision + ".json";

    /// <inheritdoc />
    public async Task<string> GetManifestFileAsync(string revision, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        if (!IconFileCache.IsRevision(revision))
        {
            return null;
        }

        var directory = Path.Combine(_options.CachePath, "kits");
        var path = Path.Combine(directory, "manifest-" + revision + ".json");
        if (File.Exists(path))
        {
            return path;
        }

        var catalog = _catalogAccessor.Catalog;
        var manifest = _planner.GetManifest(catalog);
        if (manifest.Revision != revision)
        {
            return null;
        }

        var published = await IconFileCache.PublishAsync(path, async (stream, token) =>
        {
            await stream.WriteAsync(manifest.Content, token);
            if (catalog.ChangeToken.HasChanged)
            {
                throw new IOException("Icon sources changed during manifest creation. Retry after catalog reload.");
            }
        }, cancelToken);
        await RegisterArtifactAsync(catalog, new("manifest", null, null, null, "kits/manifest-" + revision + ".json"));
        return published;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IconKit> Kits => _catalogAccessor.Catalog.Kits.Values;

    /// <inheritdoc />
    public IReadOnlySet<string> GetSourceAddresses()
    {
        var catalog = _catalogAccessor.Catalog;
        var addresses = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kit in catalog.Kits.Values)
        {
            var library = catalog.SelectLibrary(kit.DefaultLibrary);
            var variant = catalog.SelectVariant(library, null, kit);
            if (variant == null)
            {
                continue;
            }

            var index = GetIndex(catalog, library, variant);
            if (index.CanGenerate(kit.Name))
            {
                foreach (var entry in index.GetEntries(kit.Name))
                {
                    addresses.Add(entry.CreateInfo().Address);
                }
            }
        }

        return addresses;
    }

    /// <inheritdoc />
    public IconKitReference GetReference(IconInfo icon)
    {
        Guard.NotNull(icon);

        var catalog = _catalogAccessor.Catalog;
        var index = GetIndex(catalog, icon.SelectionLibraryName ?? icon.LibraryName, icon.SelectionVariantName ?? icon.VariantName);
        if (index == null || !index.Memberships.TryGetValue(
            (icon.Area, icon.ConceptName, icon.Address, icon.Transform, icon.StrokeScale), out var member))
        {
            return null;
        }

        if (member.Pinned)
        {
            var kit = catalog.Kits[member.Kit];
            var library = catalog.SelectLibrary(kit.DefaultLibrary);
            index = GetIndex(catalog, library, catalog.SelectVariant(library, null, kit));
        }
        if (!index.CanGenerate(member.Kit)) return null;
        return new IconKitReference(BuildUrl(member.Kit, index.Plans[member.Kit].Value.Revision)
            + "#" + Uri.EscapeDataString(member.Symbol));
    }

    /// <inheritdoc />
    public string GetUrl(string kitName, string library = null, string variant = null)
    {
        var catalog = _catalogAccessor.Catalog;
        if (kitName == null || !catalog.Kits.TryGetValue(kitName, out var kit))
        {
            return null;
        }

        var selectedLibrary = catalog.SelectLibrary(library ?? kit.DefaultLibrary);
        var selectedVariant = catalog.SelectVariant(selectedLibrary, variant, kit);
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

        var directory = Path.Combine(_options.CachePath, "kits");
        var path = Path.Combine(directory, $"{kitName}-{revision}.svg");
        // Retain historical revisions for already rendered pages, even after sources change.
        // Nothing is read into memory and no source plan is needed for an existing file.
        if (File.Exists(path))
        {
            return path;
        }

        var catalog = _catalogAccessor.Catalog;
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

        var published = await plan.PublishAsync(path, cancelToken);
        await RegisterArtifactAsync(catalog, new("kit", kitName, plan.LibraryName, plan.VariantName,
            "kits/" + kitName + "-" + plan.Revision + ".svg"));
        return published;
    }

    /// <inheritdoc />
    public async Task<IconBrowserResult> SearchAsync(string kitName, IconSearchQuery query, CancellationToken cancelToken = default)
    {
        Guard.NotEmpty(kitName);
        Guard.NotNull(query);
        Guard.NotNegative(query.Skip);
        Guard.InRange(query.Take, 1, 500);

        var catalog = _catalogAccessor.Catalog;
        var sourceSnapshot = catalog.ChangeToken;
        if (!catalog.Kits.TryGetValue(kitName, out var kit))
        {
            return null;
        }

        var library = catalog.SelectLibrary(kit.DefaultLibrary);
        var variant = catalog.SelectVariant(library, null, kit);
        var index = GetIndex(catalog, library, variant);
        var sprite = index.Plans[kitName].Value;
        var path = Path.Combine(_options.CachePath, "kits", kitName + "-" + sprite.Revision + ".svg");
        await sprite.PublishAsync(path, cancelToken);
        await RegisterArtifactAsync(catalog, new("kit", kitName, sprite.LibraryName, sprite.VariantName,
            "kits/" + kitName + "-" + sprite.Revision + ".svg"));

        // Search the selected kit, not global concept precedence. Empty searches load no tags.
        var page = query.FindPage(index.GetAvailableEntries(kitName), (entry, word) =>
            entry.Concept.Contains(word, StringComparison.OrdinalIgnoreCase)
            || entry.Mapping.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || (entry.Library.Tags.GetValueOrDefault(entry.Mapping.Name) ?? [])
                .Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase)));

        if (sourceSnapshot.HasChanged)
        {
            throw new IOException("Icon sources changed during browsing. Retry with the current catalog.");
        }

        return new IconBrowserResult(BuildUrl(kitName, sprite.Revision), page.TotalCount,
            page.Items.Select(entry => IconBrowserItem.Create(entry.CreateInfo(), entry.Concept,
                catalog.ExternalIcons[kitName].Contains(entry.Concept))).ToArray());
    }

    // Finish tracking published files even if the originating request was cancelled.
    private Task RegisterArtifactAsync(IconCatalog catalog, IconCacheArtifact artifact)
        => _lifecycle.TrackCacheFileAsync(artifact, catalog, CancellationToken.None);

    private string BuildUrl(string kit, string revision)
        => (_httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + _options.RoutePrefix + "/" + Uri.EscapeDataString(kit) + "-" + revision + ".svg";

    // Rebuild plans when a requested sprite revision is not yet known in memory.
    // Does not enumerate the cache directory or parse SVG drawings. Incomplete library variants
    // cannot supply this kit and are skipped; normal URL generation still reports missing targets.
    private bool RecoverPlans(IconCatalog catalog, string kit)
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

    private IconKitIndex GetIndex(IconCatalog catalog, string library, string variant)
        => _planner.GetIndex(catalog, library, variant);

    private IconKitIndex GetIndex(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
        => _planner.GetIndex(catalog, library.Manifest.SystemName, variant.Manifest.Name);
}
