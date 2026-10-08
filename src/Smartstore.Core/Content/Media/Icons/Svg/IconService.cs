using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Loads local SVG libraries lazily and keeps the existing font icon explorer independent.
/// </summary>
/// <param name="applicationContext">Provides the application data file system containing the Icons source directory.</param>
/// <param name="cache">Stores prepared SVG payloads by canonical address and content revision.</param>
public sealed class IconService(IApplicationContext applicationContext, IIconCache cache) : IIconService
{
    private readonly Lock _sync = new();
    private IconCatalog _catalog;

    /// <summary>
    /// Gets the shared source generation used by individual icons and kits.
    /// </summary>
    internal IconCatalog Catalog
    {
        get
        {
            lock (_sync)
            {
                // Publish validated manifests; supplementary data loads independently on demand.
                // Existing requests retain their generation while the next request sees changes.
                if (_catalog == null || _catalog.ChangeToken.HasChanged)
                {
                    _catalog = IconCatalog.Load(applicationContext.AppDataRoot);
                }

                return _catalog;
            }
        }
    }

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
        cancelToken.ThrowIfCancellationRequested();
        IReadOnlyList<IconLibrary> result = Catalog.Libraries.Values.Distinct().Select(x => x.Manifest).ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IconInfo> GetIconAsync(string name, string library = null, string variant = null, CancellationToken cancelToken = default)
    {
        cancelToken.ThrowIfCancellationRequested();
        return Task.FromResult(Find(Catalog, name, library, variant));
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
        var library = SelectLibrary(catalog, address.Library);
        var variant = SelectVariant(catalog, library, address.Variant);
        var source = variant?.GetSource(address.Name);
        if (source == null)
        {
            return null;
        }

        // Rebuild the canonical identity from current manifests, not the caller's mutable Address.
        var info = CreateInfo(library, variant, address.Name, false);

        // The persisted address describes identity; the internal revision describes prepared
        // content. A parser change must invalidate payloads even when source files are unchanged.
        var revision = IconSvgParser.Revision + source.Revision;
        var key = info.Address + ":" + revision;
        var svg = await cache.GetAsync(key, cancelToken);
        if (svg != null)
        {
            return svg;
        }

        using (await AsyncLock.KeyedAsync("icons:svg:" + key, cancelToken: cancelToken))
        {
            // Another request may have filled the cache while we waited. This lock coalesces
            // misses within this process only; other nodes may safely prepare the same payload.
            svg = await cache.GetAsync(key, cancelToken);
            if (svg == null)
            {
                cancelToken.ThrowIfCancellationRequested();
                svg = variant.Prepare(info, source, revision);
                // Unlike an in-memory source snapshot, files can change while being read.
                // Do not publish a prepared value after this index generation was invalidated.
                if (catalog.ChangeToken.HasChanged)
                {
                    throw new IOException("Icon sources changed during preparation. Retry after catalog reload.");
                }

                await cache.PutAsync(key, svg, cancelToken);
            }

            // Immutable payloads can be shared directly with callers.
            return svg;
        }
    }

    /// <inheritdoc />
    public Task<IconSearchResult> SearchAsync(IconSearchQuery query, CancellationToken cancelToken = default)
    {
        Guard.NotNull(query);

        cancelToken.ThrowIfCancellationRequested();
        Guard.NotNegative(query.Skip);

        Guard.InRange(query.Take, 1, 500);

        var catalog = Catalog;
        var library = SelectLibrary(catalog, query.Library);
        var variant = SelectVariant(catalog, library, query.Variant);
        if (variant == null)
        {
            return Task.FromResult(new IconSearchResult());
        }

        var words = (query.Term ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        // Reverse the conceptual mapping for search, but enumerate actual available icons.
        // Multiple concepts pointing to one icon should produce one result, not duplicates.
        var aliases = library.Mapping.ToLookup(x => x.Value.Name, x => x.Key, StringComparer.Ordinal);
        var matches = new List<IconInfo>();
        foreach (var name in variant.Names.OrderBy(x => x, StringComparer.Ordinal))
        {
            cancelToken.ThrowIfCancellationRequested();
            var tags = library.Tags.GetValueOrDefault(name) ?? [];
            if (words.All(word => name.Contains(word, StringComparison.OrdinalIgnoreCase)
                || tags.Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase))
                || aliases[name].Any(alias => alias.Contains(word, StringComparison.OrdinalIgnoreCase))))
            {
                matches.Add(CreateInfo(library, variant, name));
            }
        }

        return Task.FromResult(new IconSearchResult
        {
            TotalCount = matches.Count,
            Items = matches.Skip(query.Skip).Take(query.Take).ToArray()
        });
    }

    private static IconInfo Find(IconCatalog catalog, string name, string libraryName, string variantName)
    {
        Guard.NotEmpty(name);

        var separator = name.IndexOf('?');
        var modifiers = separator < 0 ? default : IconModifiers.Parse(name.AsSpan(separator + 1), name);
        var address = IconAddress.Parse(separator < 0 ? name : name[..separator]);
        // Explicit address components take precedence; method arguments fill missing components.
        // Compare resolved objects so a full name and its short name are not treated as a conflict.
        var kit = address.SkipMapping ? null : catalog.ConceptKits.GetValueOrDefault(address.Name);
        var library = SelectLibrary(catalog, address.Library ?? libraryName ?? kit?.DefaultLibrary);
        if (address.Library != null && libraryName != null && library != SelectLibrary(catalog, libraryName))
        {
            throw new ArgumentException("The address and library parameter select different libraries.", nameof(libraryName));
        }

        if (address.Variant != null && variantName != null
            && SelectVariant(catalog, library, address.Variant) != SelectVariant(catalog, library, variantName))
        {
            throw new ArgumentException("The address and variant parameter select different variants.", nameof(variantName));
        }

        var variant = SelectVariant(catalog, library, address.Variant ?? variantName, kit);
        if (variant == null)
        {
            return default;
        }

        // Resolve exactly one mapping, not an alias chain. If its target is absent, return null;
        // falling back to the original name would silently ignore a broken customization.
        // Explicit direct addresses also avoid loading mapping.json.
        var mapping = address.SkipMapping ? null : library.Mapping.GetValueOrDefault(address.Name);
        var actualName = mapping?.Name ?? address.Name;
        var source = variant.GetSource(actualName);
        if (source == null)
        {
            return null;
        }

        var info = CreateInfo(library, variant, actualName);
        var mappingTransform = mapping?.Transform ?? default;
        info.Transform = modifiers.Apply(mappingTransform);
        info.RequiresInline = info.Transform != mappingTransform;
        info.StrokeScale = modifiers.StrokeScale ?? mapping?.StrokeScale ?? 1;
        return info;
    }

    /// <summary>
    /// Selects a registered library using the shared default rules.
    /// </summary>
    /// <param name="catalog">The source generation.</param>
    /// <param name="name">The optional library selector.</param>
    internal static IconCatalog.Library SelectLibrary(IconCatalog catalog, string name)
        => name == null ? catalog.DefaultLibrary : catalog.Libraries.GetValueOrDefault(name);

    /// <summary>
    /// Selects a variant, applying the root override only to the default library.
    /// </summary>
    /// <param name="catalog">The source generation.</param>
    /// <param name="library">The selected library, or null for an unknown selector.</param>
    /// <param name="name">The optional variant selector.</param>
    /// <param name="kit">The optional conceptual kit supplying defaults before the global fallback.</param>
    internal static IconCatalog.Variant SelectVariant(IconCatalog catalog, IconCatalog.Library library, string name, IconKit kit = null)
    {
        if (library == null)
        {
            return null;
        }

        // A kit's variant is scoped to its effective default library, even when an
        // explicit selector chooses a different library with a similarly named variant.
        if (name == null && kit?.DefaultVariant != null
            && library == SelectLibrary(catalog, kit.DefaultLibrary))
        {
            name = kit.DefaultVariant;
        }

        // The global variant override belongs only to the default library. Applying it to
        // another library could select an unrelated style or a variant that does not exist.
        name ??= library == catalog.DefaultLibrary ? catalog.DefaultVariant ?? library.Manifest.DefaultVariant : library.Manifest.DefaultVariant;
        return library.Variants.GetValueOrDefault(name);
    }

    /// <summary>
    /// Creates the canonical identity without reading SVG drawing data.
    /// </summary>
    /// <param name="library">The selected library.</param>
    /// <param name="variant">The selected variant.</param>
    /// <param name="name">The actual source icon name.</param>
    /// <param name="includeTags">Whether to attach deferred search metadata.</param>
    internal static IconInfo CreateInfo(IconCatalog.Library library, IconCatalog.Variant variant, string name, bool includeTags = true) => new()
    {
        Address = new IconAddress(name, library.Manifest.ShortName ?? library.Manifest.SystemName, variant.Manifest.ShortName ?? variant.Manifest.Name).ToString(),
        LibraryName = library.Manifest.SystemName,
        LibraryShortName = library.Manifest.ShortName,
        VariantName = variant.Manifest.Name,
        VariantShortName = variant.Manifest.ShortName,
        Name = name,
        DeferredTags = includeTags
            ? new Lazy<string[]>(() => library.Tags.TryGetValue(name, out var tags) ? (string[])tags.Clone() : [])
            : null
    };
}
