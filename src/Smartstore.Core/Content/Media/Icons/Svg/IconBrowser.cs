using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Adapts kit and library operations to picker results without owning search or sprite generation.
/// </summary>
/// <param name="icons">Provides the shared catalog, library search and variant sprites.</param>
/// <param name="kits">Provides kit search and sprite generation.</param>
/// <param name="httpContextAccessor">Provides the current request path base.</param>
public sealed class IconBrowser(IconService icons, IconKitService kits, IHttpContextAccessor httpContextAccessor) : IIconBrowser
{
    /// <inheritdoc />
    public IconBrowserSource GetSource(string address)
    {
        var catalog = icons.Catalog;
        IconAddress parsed = default;
        if (!string.IsNullOrEmpty(address))
        {
            var separator = address.IndexOf('?');
            if (!IconAddress.TryParse(separator < 0 ? address : address[..separator], out parsed))
            {
                return null;
            }
        }

        // Use the same concept priority as resolution and the manifest, including
        // concepts omitted from the client manifest because they need inline rendering.
        if (parsed.IsEmpty && catalog.Kits.ContainsKey("shared"))
        {
            return new IconBrowserSource("shared", null, null);
        }
        if (!parsed.IsEmpty && parsed.Library == null && !parsed.SkipMapping
            && catalog.ConceptKits.TryGetValue(parsed.Name, out var kit))
        {
            return new IconBrowserSource(kit.Name, null, null);
        }

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

        cancelToken.ThrowIfCancellationRequested();
        var catalog = icons.Catalog;
        IconBrowserResult result;
        if (kit != null)
        {
            if (query.Library != null || query.Variant != null)
            {
                throw new ArgumentException("Kit browsing cannot also select a library or variant.", nameof(query));
            }

            var page = await kits.SearchAsync(catalog, kit, query, cancelToken);
            if (page == null)
            {
                return null;
            }

            result = new IconBrowserResult(page.Value.Url, page.Value.TotalCount,
                page.Value.Items.Select(entry => CreateItem(entry.Info, entry.Concept)).ToArray());
        }
        else
        {
            var (library, variant) = SelectSource(catalog, query.Library, query.Variant);
            if (variant == null)
            {
                return null;
            }

            var revision = await icons.PrepareSpriteAsync(catalog, library, variant, cancelToken);
            var page = IconService.Search(query, library, variant);
            var url = (httpContextAccessor.HttpContext?.Request.PathBase.Value ?? string.Empty)
                + "/icons/browser/" + Uri.EscapeDataString(library.Manifest.SystemName)
                + "/" + Uri.EscapeDataString(variant.Manifest.Name) + "/" + revision + ".svg";
            result = new IconBrowserResult(url, page.TotalCount, page.Items.Select(info => CreateItem(info)).ToArray());
        }

        if (catalog.ChangeToken.HasChanged)
        {
            throw new IOException("Icon sources changed during browsing. Retry with the current catalog.");
        }

        return result;
    }

    /// <inheritdoc />
    public Task<string> GetSpriteFileAsync(string library, string variant, string revision, CancellationToken cancelToken = default)
    {
        var catalog = icons.Catalog;
        var source = SelectSource(catalog, library, variant);
        return source.Variant == null ? Task.FromResult<string>(null)
            : icons.GetSpriteFileAsync(catalog, source.Library, source.Variant, revision, cancelToken);
    }

    /// <summary>
    /// Selects registered sources while keeping the internal system library out of the picker.
    /// </summary>
    /// <param name="catalog">The retained catalog generation.</param>
    /// <param name="library">The optional library selector.</param>
    /// <param name="variant">The optional variant selector.</param>
    private static (IconCatalog.Library Library, IconCatalog.Variant Variant) SelectSource(IconCatalog catalog, string library, string variant)
    {
        var selected = IconService.SelectLibrary(catalog, library);
        return selected == null || selected.Manifest.SystemName.Equals("system", StringComparison.OrdinalIgnoreCase)
            ? default : (selected, IconService.SelectVariant(catalog, selected, variant));
    }

    /// <summary>
    /// Keeps kit selections conceptual and library selections literal. Mapping modifiers are baked into sprites.
    /// </summary>
    /// <param name="info">The resolved source metadata.</param>
    /// <param name="concept">The kit concept, or null for a literal library selection.</param>
    private static IconBrowserItem CreateItem(IconInfo info, string concept = null)
        => new(concept ?? info.Name, concept ?? IconModifiers.FormatAddress(info), info.Address, info.LibraryKey, info.VariantKey,
            null);
}
