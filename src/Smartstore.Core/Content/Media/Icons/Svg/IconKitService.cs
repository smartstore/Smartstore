using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Smartstore.Json;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Resolves external sprite URLs and publishes immutable sprite files in App_Data/.cache/IconKits.
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
        if (revision == null || revision.Length != 24 || revision.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            return null;
        }

        var directory = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "IconKits");
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

        using (await AsyncLock.KeyedAsync("icons:manifest:" + path, cancelToken: cancelToken))
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(directory);
                var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    await File.WriteAllBytesAsync(temporary, manifest.Content, cancelToken);
                    if (catalog.ChangeToken.HasChanged)
                    {
                        throw new IOException("Icon sources changed during manifest creation. Retry after catalog reload.");
                    }

                    try
                    {
                        File.Move(temporary, path);
                    }
                    catch (IOException) when (File.Exists(path))
                    {
                        // Another process published the same immutable revision.
                    }
                }
                finally
                {
                    File.Delete(temporary);
                }
            }
        }

        return path;
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

    private static (string Revision, byte[] Content) CreateManifest(IconCatalog catalog)
    {
        // Export only resolution data. Search tags and SVG drawings stay server-side.
        var libraries = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var library in catalog.Libraries.Values.Distinct().OrderBy(x => x.Manifest.SystemName, StringComparer.Ordinal))
        {
            var mapping = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in library.Mapping)
            {
                var target = pair.Value.Name;
                var modifiers = new List<string>();
                var transform = pair.Value.Transform;
                if (transform.FlipX || transform.FlipY)
                {
                    modifiers.Add("flip=" + (transform.FlipX ? "x" : string.Empty) + (transform.FlipY ? "y" : string.Empty));
                }
                if (transform.Rotation != 0)
                {
                    modifiers.Add("rotate=" + transform.Rotation.ToString("R", CultureInfo.InvariantCulture));
                }
                if (pair.Value.StrokeScale != 1)
                {
                    modifiers.Add("stroke-scale=" + pair.Value.StrokeScale.ToString("R", CultureInfo.InvariantCulture));
                }
                if (modifiers.Count != 0)
                {
                    target += "?" + string.Join('&', modifiers);
                }
                if (target != pair.Key)
                {
                    mapping.Add(pair.Key, target);
                }
            }

            libraries.Add(library.Manifest.SystemName, new
            {
                library.Manifest.ShortName,
                library.Manifest.DefaultVariant,
                Variants = library.Variants.Values.Distinct().OrderBy(x => x.Manifest.Name, StringComparer.Ordinal)
                    .ToDictionary(x => x.Manifest.Name, x => new { x.Manifest.ShortName }),
                Mapping = mapping
            });
        }

        var kits = catalog.Kits.Values.OrderBy(x => x.Name, StringComparer.Ordinal).ToDictionary(x => x.Name,
            x => new { x.DefaultLibrary, x.DefaultVariant, Concepts = x.Icons.OrderBy(n => n, StringComparer.Ordinal).ToArray() });
        var urls = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
        // Only effective kit defaults are advertised. Other combinations use the render endpoint.
        // This avoids multiplying startup work by every installed library and variant.
        foreach (var kit in catalog.Kits.Values)
        {
            var library = IconService.SelectLibrary(catalog, kit.DefaultLibrary);
            var variant = IconService.SelectVariant(catalog, library, null, kit);
            if (variant == null)
            {
                continue;
            }

            var key = (library.Manifest.ShortName ?? library.Manifest.SystemName) + "@" + (variant.Manifest.ShortName ?? variant.Manifest.Name);
            if (urls.ContainsKey(key))
            {
                continue;
            }

            var kitUrls = new SortedDictionary<string, string>(StringComparer.Ordinal);
            urls.Add(key, kitUrls);
            var index = GetIndex(catalog, library, variant);
            foreach (var candidate in catalog.Kits.Values)
            {
                if (candidate.Icons.All(name => variant.GetSource(library.Mapping.GetValueOrDefault(name)?.Name ?? name) != null))
                {
                    kitUrls.Add(candidate.Name, "icons/" + Uri.EscapeDataString(candidate.Name) + "-" + index.Plans[candidate.Name].Value.Revision + ".svg");
                }
            }
        }

        var content = JsonSerializer.SerializeToUtf8Bytes(new
        {
            SchemaVersion = 2,
            DefaultLibrary = catalog.DefaultLibrary.Manifest.SystemName,
            catalog.DefaultVariant,
            Libraries = libraries,
            Kits = kits,
            Urls = urls
        }, SmartJsonOptions.CamelCased);
        // Hash exact UTF-8 bytes so different nodes agree on immutable file identities.
        return (Convert.ToHexStringLower(SHA256.HashData(content).AsSpan(0, 12)), content);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IconKit> Kits => icons.Catalog.Kits.Values;

    /// <inheritdoc />
    public IconKitReference GetReference(IconInfo icon)
    {
        Guard.NotNull(icon);

        var index = GetIndex(icon.LibraryName, icon.VariantName);
        if (index == null || !index.Memberships.TryGetValue((icon.Name, icon.Transform), out var member))
        {
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
        if (!IconAddress.IsQualifier(kitName) || revision == null || revision.Length != 24 || revision.Any(c => !(c is >= '0' and <= '9' or >= 'a' and <= 'f')))
        {
            return null;
        }

        var directory = Path.Combine(applicationContext.AppDataRoot.Root, ".cache", "IconKits");
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

        using (await AsyncLock.KeyedAsync("icons:kit:" + path, cancelToken: cancelToken))
        {
            if (File.Exists(path))
            {
                return path;
            }

            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                // Write beside the destination so publication is an atomic rename on the same volume.
                // Only the current icon's XML tree is held while the sprite streams to disk.
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    plan.Write(stream, cancelToken);
                }

                cancelToken.ThrowIfCancellationRequested();
                try
                {
                    File.Move(temporaryPath, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    // Another process sharing this directory published the identical revision first.
                }
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        return path;
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
            var names = catalog.Kits[kit].Icons.Select(x => library.Mapping.GetValueOrDefault(x)?.Name ?? x).ToArray();
            foreach (var variant in library.Variants.Values.Distinct())
            {
                if (names.All(x => variant.GetSource(x) != null))
                {
                    // Evaluating a plan registers its fingerprint in the owning catalog.
                    _ = GetIndex(catalog, library, variant).Plans[kit].Value;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Selects only registered manifests, bounding index entries to configured variants.
    /// </summary>
    /// <param name="library">The library selector.</param>
    /// <param name="variant">The variant selector.</param>
    private IconKitIndex GetIndex(string library, string variant)
    {
        var catalog = icons.Catalog;
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
    private static IconKitIndex GetIndex(IconCatalog catalog, IconCatalog.Library library, IconCatalog.Variant variant)
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
