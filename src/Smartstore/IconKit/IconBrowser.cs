using Smartstore.Engine;
using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Smartstore.IO;

namespace Smartstore.IconKit;

public sealed class IconBrowser : IIconBrowser
{
    private readonly IIconCatalogAccessor _catalogAccessor;
    private readonly IIconSpritePlanner _planner;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IconKitOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IFileSystem _fileCache;

    public IconBrowser(
        IApplicationContext appContext,
        IIconCatalogAccessor catalogAccessor,
        IIconSpritePlanner planner,
        IIconCacheLifecycle lifecycle,
        IOptions<IconKitOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _catalogAccessor = catalogAccessor;
        _planner = planner;
        _lifecycle = lifecycle;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;

        _fileCache = IconFileHelper.CreateFileSystem(appContext, options.Value.CachePath);
    }

    /// <inheritdoc />
    public IconBrowserSource GetSource(string address, string area = "public", bool showHiddenKits = false)
    {
        var catalog = _catalogAccessor.Catalog;
        IconAddress parsed = default;
        if (!string.IsNullOrEmpty(address))
        {
            var separator = address.IndexOf('?');
            if (!IconAddress.TryParse(separator < 0 ? address : address[..separator], out parsed))
            {
                return null;
            }
        }

        // Empty pickers start with the first visible kit in metadata order, regardless of area.
        // Existing concepts retain their area-specific resolution priority.
        var kit = parsed.IsEmpty
            ? catalog.Kits.Values.FirstOrDefault(x => x.ShowInPicker || showHiddenKits)
            : parsed.Library == null && !parsed.SkipMapping ? catalog.FindKit(parsed.Name, area) : null;
        if (kit != null) return new IconBrowserSource(kit.Name, null, null, !parsed.IsEmpty && catalog.ExternalIcons[kit.Name].Contains(parsed.Name));

        var source = SelectSource(catalog, parsed.Library, parsed.Variant);
        return source.Variant == null ? null
            : new IconBrowserSource(null, source.Library.Manifest.SystemName, source.Variant.Manifest.Name);
    }

    /// <inheritdoc />
    public async Task<IconBrowserResult> SearchAsync(IconSearchQuery query, string kit = null, CancellationToken cancelToken = default)
    {
        Guard.NotNull(query);
        Guard.NotNegative(query.Skip);
        Guard.InRange(query.Take, 1, 500);

        if (kit != null)
        {
            if (query.Library != null || query.Variant != null)
            {
                throw new ArgumentException("Kit browsing cannot also select a library or variant.", nameof(query));
            }

            return await SearchKitAsync(_catalogAccessor.Catalog, kit, query, cancelToken);
        }

        var catalog = _catalogAccessor.Catalog;
        var (library, variant) = SelectSource(catalog, query.Library, query.Variant);
        var sourceSnapshot = catalog.ChangeToken;
        if (variant == null)
        {
            return null;
        }

        var sprite = _planner.GetLibrarySprite(catalog, library.Manifest.SystemName, variant.Manifest.Name);
        await PrepareSpriteAsync(catalog, sprite, CreateBrowserArtifact(sprite), cancelToken);
        var page = catalog.Search(query);
        var url = BuildUrl("browser/" + Uri.EscapeDataString(library.Manifest.SystemName)
            + "/" + Uri.EscapeDataString(variant.Manifest.Name) + "/" + sprite.Revision + ".svg");
        var result = new IconBrowserResult(url, page.TotalCount, page.Items.Select(info => IconBrowserItem.Create(info)).ToArray());

        if (sourceSnapshot.HasChanged)
        {
            throw new IOException("Icon sources changed during browsing. Retry with the current catalog.");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetSpriteFileAsync(string library, string variant, string revision, CancellationToken cancelToken = default)
    {
        var catalog = _catalogAccessor.Catalog;
        var source = SelectSource(catalog, library, variant);
        if (source.Variant == null || !IconFileHelper.IsValidRevisionHash(revision))
        {
            return null;
        }

        library = source.Library.Manifest.SystemName;
        variant = source.Variant.Manifest.Name;
        var file = _fileCache.GetFile("browser/" + library + "-" + variant + "-" + revision + ".svg");

        // Serve historical files without marking them as the current revision again.
        if (file.Exists)
        {
            return file.PhysicalPath;
        }

        var sprite = _planner.GetLibrarySprite(catalog, library, variant);
        if (sprite.Revision != revision)
        {
            return null;
        }

        await PrepareSpriteAsync(catalog, sprite, CreateBrowserArtifact(sprite), cancelToken);
        return file.PhysicalPath;
    }

    private async Task<IconBrowserResult> SearchKitAsync(IconCatalog catalog, string kitName, IconSearchQuery query, CancellationToken cancelToken)
    {
        Guard.NotEmpty(kitName);

        var sourceSnapshot = catalog.ChangeToken;
        if (!catalog.Kits.TryGetValue(kitName, out var kit))
        {
            return null;
        }

        var library = catalog.SelectLibrary(kit.DefaultLibrary);
        var variant = catalog.SelectVariant(library, null, kit);
        var index = _planner.GetIndex(catalog, library.Manifest.SystemName, variant.Manifest.Name);
        var sprite = index.Plans[kitName].Value;
        await PrepareSpriteAsync(catalog, sprite, new("kit", kitName, sprite.LibraryName, sprite.VariantName,
            "kits/" + kitName + "-" + sprite.Revision + ".svg"), cancelToken);

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

        return new IconBrowserResult(BuildUrl(Uri.EscapeDataString(kitName) + "-" + sprite.Revision + ".svg"), page.TotalCount,
            page.Items.Select(entry => IconBrowserItem.Create(entry.CreateInfo(), entry.Concept,
                catalog.ExternalIcons[kitName].Contains(entry.Concept))).ToArray());
    }

    private async Task PrepareSpriteAsync(IconCatalog catalog, IconSprite sprite, IconCacheArtifact artifact, CancellationToken cancelToken)
    {
        await sprite.PublishAsync(_fileCache.GetFile(artifact.File), cancelToken);

        // Track published files even if the originating request was cancelled.
        await _lifecycle.TrackCacheFileAsync(artifact, catalog, CancellationToken.None);
    }

    private static IconCacheArtifact CreateBrowserArtifact(IconSprite sprite)
        => new("browser", null, sprite.LibraryName, sprite.VariantName,
            $"browser/{sprite.LibraryName}-{sprite.VariantName}-{sprite.Revision}.svg");

    private string BuildUrl(string path)
        => (_httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty) + _options.RoutePrefix + "/" + path;

    // Selects registered sources while keeping the internal system library out of the picker.
    private static (IconCatalog.Library Library, IconCatalog.Variant Variant) SelectSource(IconCatalog catalog, string library, string variant)
    {
        var selected = catalog.SelectLibrary(library);
        return selected == null || selected.Manifest.SystemName.Equals("system", StringComparison.OrdinalIgnoreCase)
            ? default : (selected, catalog.SelectVariant(selected, variant));
    }

}
