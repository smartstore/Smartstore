using Smartstore.Engine;
using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Smartstore.IO;

namespace Smartstore.IconKit;

public sealed class IconKitService : IIconKitService
{
    private readonly IIconCatalogAccessor _catalogAccessor;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IIconSpritePlanner _planner;
    private readonly IconKitOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileSystem _fileCache;

    public IconKitService(
        IApplicationContext appContext,
        IIconCatalogAccessor catalogAccessor,
        IIconCacheLifecycle lifecycle,
        IIconSpritePlanner planner,
        IOptions<IconKitOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _catalogAccessor = catalogAccessor;
        _lifecycle = lifecycle;
        _planner = planner;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;

        _fileCache = IconFileHelper.CreateFileSystem(appContext, options.Value.CachePath);
    }

    /// <inheritdoc />
    public string GetManifestUrl()
        => (_httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + _options.RoutePrefix + "/manifest/" + _planner.GetManifest(_catalogAccessor.Catalog).Revision + ".json";

    /// <inheritdoc />
    public async Task<string> GetManifestFileAsync(string revision, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        if (!IconFileHelper.IsValidRevisionHash(revision))
        {
            return null;
        }

        var file = _fileCache.GetFile("kits/manifest-" + revision + ".json");
        if (file.Exists)
        {
            return file.PhysicalPath;
        }

        var catalog = _catalogAccessor.Catalog;
        var manifest = _planner.GetManifest(catalog);
        if (manifest.Revision != revision)
        {
            return null;
        }

        var published = await IconFileHelper.GetOrCreateCacheFileAsync(file, async (stream, token) =>
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
    public string GetSpriteHref(IconInfo icon)
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
        return BuildUrl(member.Kit, index.Plans[member.Kit].Value.Revision)
            + "#" + Uri.EscapeDataString(member.Symbol);
    }

    /// <inheritdoc />
    public string GetUrl(string kitName, string library = null, string variant = null)
    {
        var sprite = GetSpritePlan(_catalogAccessor.Catalog, kitName, library, variant);
        return sprite == null ? null : BuildUrl(kitName, sprite.Revision);
    }

    /// <inheritdoc />
    public IReadOnlyList<IconKitFileInfo> GetCachedSpriteFiles()
    {
        var catalog = _catalogAccessor.Catalog;
        var files = new List<IconKitFileInfo>();

        foreach (var kit in catalog.Kits.Values.OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            var sprite = GetSpritePlan(catalog, kit.Name, null, null);
            if (sprite == null)
            {
                continue;
            }

            var file = _fileCache.GetFile($"kits/{kit.Name}-{sprite.Revision}.svg");
            if (file.Exists)
            {
                files.Add(new IconKitFileInfo(kit.Name, kit.DisplayName ?? kit.Name, file.Name, file.Length));
            }
        }

        return files;
    }

    /// <inheritdoc />
    public async Task<string> GetSpriteFileAsync(string kitName, string revision, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        // Only revision hashes may enter physical filenames; arbitrary request input must
        // neither escape the cache directory nor trigger generation for invented revisions.
        if (!IconAddress.IsQualifier(kitName) || !IconFileHelper.IsValidRevisionHash(revision))
        {
            return null;
        }

        var file = _fileCache.GetFile($"kits/{kitName}-{revision}.svg");
        // Retain historical revisions for already rendered pages, even after sources change.
        // Nothing is read into memory and no source plan is needed for an existing file.
        if (file.Exists)
        {
            return file.PhysicalPath;
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

        var published = await plan.PublishAsync(file, cancelToken);
        await RegisterArtifactAsync(catalog, new("kit", kitName, plan.LibraryName, plan.VariantName,
            "kits/" + kitName + "-" + plan.Revision + ".svg"));
        return published;
    }

    // Finish tracking published files even if the originating request was cancelled.
    private Task RegisterArtifactAsync(IconCatalog catalog, IconCacheArtifact artifact)
        => _lifecycle.TrackCacheFileAsync(artifact, catalog, CancellationToken.None);

    private IconSprite GetSpritePlan(IconCatalog catalog, string kitName, string library, string variant)
    {
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
        return index.Plans[kitName].Value;
    }

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
