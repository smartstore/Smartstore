using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using Smartstore.IconKit.Sprites;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Smartstore.IconKit;

public sealed class IconBrowser : IIconBrowser
{
    private readonly IIconCatalogAccessor _catalogAccessor;
    private readonly IIconKitService _kits;
    private readonly IIconService _icons;
    private readonly IIconLibrarySpriteService _librarySprites;
    private readonly IIconCacheLifecycle _lifecycle;
    private readonly IconKitOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IconBrowser(
        IIconCatalogAccessor catalogAccessor,
        IIconKitService kits,
        IIconService icons,
        IIconLibrarySpriteService librarySprites,
        IIconCacheLifecycle lifecycle,
        IOptions<IconKitOptions> options,
        IHttpContextAccessor httpContextAccessor)
    {
        _catalogAccessor = catalogAccessor;
        _kits = kits;
        _icons = icons;
        _librarySprites = librarySprites;
        _lifecycle = lifecycle;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
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

            return await _kits.SearchAsync(kit, query, cancelToken);
        }

        var catalog = _catalogAccessor.Catalog;
        var (library, variant) = SelectSource(catalog, query.Library, query.Variant);
        if (variant == null)
        {
            return null;
        }

        var revision = await _librarySprites.PrepareAsync(catalog, library.Manifest.SystemName, variant.Manifest.Name, cancelToken);
        await _lifecycle.TrackCacheFileAsync(new("browser", null, library.Manifest.SystemName, variant.Manifest.Name,
            "browser/" + library.Manifest.SystemName + "-" + variant.Manifest.Name + "-" + revision + ".svg"),
            catalog, cancelToken);
        var page = await _icons.SearchAsync(catalog, query, cancelToken);
        var url = (_httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
            + _options.RoutePrefix + "/browser/" + Uri.EscapeDataString(library.Manifest.SystemName)
            + "/" + Uri.EscapeDataString(variant.Manifest.Name) + "/" + revision + ".svg";
        var result = new IconBrowserResult(url, page.TotalCount, page.Items.Select(info => IconBrowserItem.Create(info)).ToArray());

        if (catalog.ChangeToken.HasChanged)
        {
            throw new IOException("Icon sources changed during browsing. Retry with the current catalog.");
        }

        return result;
    }

    /// <inheritdoc />
    public Task<string> GetSpriteFileAsync(string library, string variant, string revision, CancellationToken cancelToken = default)
    {
        var catalog = _catalogAccessor.Catalog;
        var source = SelectSource(catalog, library, variant);
        return source.Variant == null ? Task.FromResult<string>(null)
            : _librarySprites.GetFileAsync(catalog, source.Library.Manifest.SystemName, source.Variant.Manifest.Name, revision, cancelToken);
    }

    // Selects registered sources while keeping the internal system library out of the picker.
    private static (IconCatalog.Library Library, IconCatalog.Variant Variant) SelectSource(IconCatalog catalog, string library, string variant)
    {
        var selected = catalog.SelectLibrary(library);
        return selected == null || selected.Manifest.SystemName.Equals("system", StringComparison.OrdinalIgnoreCase)
            ? default : (selected, catalog.SelectVariant(selected, variant));
    }

}
