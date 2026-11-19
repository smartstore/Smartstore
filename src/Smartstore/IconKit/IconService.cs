using Smartstore.IconKit.Cache;
using Smartstore.IconKit.Sources;
using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;
using Smartstore.Threading;

namespace Smartstore.IconKit;

public sealed class IconService : IIconService
{
    private readonly IIconCatalogAccessor _catalogAccessor;
    private readonly IIconCache _cache;
    private readonly IIconKitService _kits;
    private readonly IIconAreaResolver _areaResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IconService(
        IIconCatalogAccessor catalogAccessor,
        IIconCache cache,
        IIconKitService kits,
        IIconAreaResolver areaResolver,
        IHttpContextAccessor httpContextAccessor)
    {
        _catalogAccessor = catalogAccessor;
        _cache = cache;
        _kits = kits;
        _areaResolver = areaResolver;
        _httpContextAccessor = httpContextAccessor;
    }

    internal IconCatalog Catalog => _catalogAccessor.Catalog;

    /// <inheritdoc />
    public IconLibrary DefaultLibrary => Catalog.DefaultLibrary.Manifest;

    /// <inheritdoc />
    public IconLibrary GetLibrary(string nameOrShortName)
    {
        Guard.NotEmpty(nameOrShortName);

        return Catalog.Libraries.TryGetValue(nameOrShortName, out var library) ? library.Manifest : null;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IconLibrary>> GetLibrariesAsync(CancellationToken cancelToken = default)
    {
        IReadOnlyList<IconLibrary> result = Catalog.Libraries.Values.Distinct().Where(x => !x.IsProvider).Select(x => x.Manifest).ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public int GetIconCount(string library = null, string variant = null)
    {
        var catalog = Catalog;
        return catalog.SelectVariant(catalog.SelectLibrary(library), variant)?.Names.Count ?? 0;
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetIconNames(string library = null, string variant = null)
    {
        var catalog = Catalog;
        return catalog.SelectVariant(catalog.SelectLibrary(library), variant)?.Names ?? FrozenSet<string>.Empty;
    }

    /// <inheritdoc />
    public Task<IconInfo> GetIconAsync(IconAddress address, string area = null, CancellationToken cancelToken = default)
    {
        if (address.IsEmpty)
        {
            throw new ArgumentException("An icon address is required.", nameof(address));
        }

        return Task.FromResult(Find(Catalog, address, area ?? _areaResolver.ResolveArea(_httpContextAccessor.HttpContext)));
    }

    /// <inheritdoc />
    public async Task<IconSvg> GetSvgAsync(IconInfo icon, CancellationToken cancelToken = default)
    {
        Guard.NotNull(icon);
        Guard.NotEmpty(icon.LibraryName);
        Guard.NotEmpty(icon.VariantName);

        cancelToken.ThrowIfCancellationRequested();

        // Validate the supplied identity before using its name in provider-relative paths.
        // IconInfo can come from search or a previous generation; do not remap its actual name.
        var address = new IconAddress(icon.Name, icon.LibraryName, icon.VariantName);
        var catalog = Catalog;
        var library = catalog.SelectLibrary(address.Library);
        var variant = catalog.SelectVariant(library, address.Variant);
        var source = variant?.GetSource(address.Name);
        if (source == null)
        {
            return null;
        }

        // Rebuild the canonical identity from current manifests, not the caller's mutable Address.
        var info = IconInfo.Create(library, variant, address.Name, false);

        // The persisted address describes identity; the internal revision describes prepared
        // content. A parser change must invalidate payloads even when source files are unchanged.
        var revision = IconSvgParser.Revision + source.Revision;
        var key = info.Address + ":" + revision;
        var svg = await _cache.GetAsync(key, cancelToken);
        if (svg != null)
        {
            return svg;
        }

        using (await AsyncLock.KeyedAsync("icons:svg:" + key, cancelToken: cancelToken))
        {
            // Another request may have filled the cache while we waited. This lock coalesces
            // misses within this process only; other nodes may safely prepare the same payload.
            svg = await _cache.GetAsync(key, cancelToken);
            if (svg == null)
            {
                cancelToken.ThrowIfCancellationRequested();
                svg = variant.Prepare(info, source, revision);
                if (svg == null)
                {
                    return null;
                }

                // Unlike an in-memory source snapshot, files can change while being read.
                // Do not publish a prepared value after this index generation was invalidated.
                if (catalog.ChangeToken.HasChanged)
                {
                    throw new IOException("Icon sources changed during preparation. Retry after catalog reload.");
                }

                await _cache.PutAsync(key, svg, cancelToken);
            }

            // Immutable payloads can be shared directly with callers.
            return svg;
        }
    }

    /// <inheritdoc />
    public Task<IconSearchResult> SearchAsync(IconSearchQuery query, CancellationToken cancelToken = default)
        => Task.FromResult(Catalog.Search(query));

    /// <inheritdoc />
    public async Task<IconCacheSummary> GetCacheSummaryAsync(CancellationToken cancelToken = default)
    {
        var kitFiles = _kits.GetCachedSpriteFiles();
        var excluded = _kits.GetSourceAddresses().ToHashSet(StringComparer.Ordinal);
        var libraries = await GetLibrariesAsync(cancelToken);
        var logos = libraries.SelectMany(library => library.Variants.Values
            .Select(variant => variant.Icon ?? library.Icon).Append(library.Icon))
            .Where(address => address.HasValue()).Distinct(StringComparer.Ordinal);

        foreach (var logo in logos)
        {
            // Resolve aliases and fallbacks without loading or caching the logo's drawing.
            var icon = await GetIconAsync(IconExpression.Parse(logo).Address, cancelToken: cancelToken);
            if (icon != null)
            {
                excluded.Add(icon.Address);
            }
        }

        var cachedIcons = new Dictionary<string, IconSvg>(StringComparer.Ordinal);
        await foreach (var entry in _cache.GetEntriesAsync(cancelToken))
        {
            var address = entry.Address.ToString();
            if (excluded.Contains(address) || cachedIcons.ContainsKey(address))
            {
                continue;
            }

            // Read only existing payloads. An expired entry must not be regenerated for diagnostics.
            var svg = await _cache.GetAsync(entry.Key, cancelToken);
            if (svg != null)
            {
                cachedIcons.TryAdd(svg.Address, svg);
            }
        }

        return new IconCacheSummary(kitFiles, cachedIcons.Values.OrderBy(x => x.Address, StringComparer.Ordinal).ToArray());
    }

    private IconInfo Find(IconCatalog catalog, IconAddress address, string area)
    {
        var kit = address.SkipMapping ? null : catalog.FindKit(address.Name, area);
        var entry = kit?.SourceAddresses.GetValueOrDefault(address.Name) ?? default;
        var entryLibrary = catalog.SelectLibrary(entry.Library ?? kit?.DefaultLibrary);
        var library = catalog.SelectLibrary(address.Library ?? entry.Library ?? kit?.DefaultLibrary);

        // Source overrides supply defaults for this concept only. Caller selectors still win,
        // and a variant from a different library must never leak into that caller selection.
        var variant = catalog.SelectVariant(library,
            address.Variant ?? (library == entryLibrary ? entry.Variant : null), kit);
        if (variant == null)
        {
            return default;
        }

        // Concrete kit sources and ! addresses bypass mapping. Ordinary concepts share
        // the same fallback resolution as kit planning and browser manifest generation.
        var useSource = !entry.IsEmpty && library == entryLibrary
            && variant == catalog.SelectVariant(entryLibrary, entry.Variant, kit);
        var info = catalog.Resolve(new IconAddress(useSource ? entry.Name : address.Name,
            library.Manifest.SystemName, variant.Manifest.Name, address.SkipMapping || useSource));
        if (info == null)
        {
            return null;
        }

        // Keep the requested kit context separate from the actual artwork identity. A mixed
        // regular kit must stay regular even when this particular symbol came from solid.
        info.Area = area ?? "public";
        info.ConceptName = address.SkipMapping ? null : address.Name;
        info.SelectionLibraryName = library.Manifest.SystemName;
        info.SelectionVariantName = variant.Manifest.Name;
        var presentation = useSource ? kit.SourcePresentations.GetValueOrDefault(address.Name) : default;
        info.Transform = presentation.Apply(info.Transform);
        info.StrokeScale = info.SupportsStrokeScaling ? presentation.StrokeScale ?? info.StrokeScale : 1;
        info.MirrorInRtl = !address.SkipMapping && address.Library == null && address.Variant == null
            && catalog.MirrorInRtl.Contains(address.Name);
        return info;
    }

}
