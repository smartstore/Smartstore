using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Smartstore.Json;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Holds library manifests and demand-loaded indexes for one file watcher generation.
/// No file objects, SVG contents or open archive handles are retained.
/// </summary>
internal sealed class IconCatalog
{
    /// <summary>
    /// Holds the small browser index once per catalog generation, without request-specific URLs.
    /// </summary>
    internal Lazy<(string Revision, byte[] Content)> BrowserManifest;

    /// <summary>
    /// Gets kit definitions from the root configuration, without loading artwork.
    /// </summary>
    internal Dictionary<string, IconKit> Kits { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the preferred kit by concept before library selection: shared, then ordinal kit name.
    /// </summary>
    internal Dictionary<string, IconKit> ConceptKits { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Keeps lazily resolved kit indexes only for this source generation.
    /// </summary>
    internal ConcurrentDictionary<string, Lazy<IconKitIndex>> KitIndexes { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Indexes already resolved plans by kit and opaque revision.
    /// </summary>
    internal ConcurrentDictionary<string, IconKitIndex.Plan> KitRevisions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Bounds cold endpoint recovery to one metadata scan per configured kit and generation.
    /// </summary>
    internal ConcurrentDictionary<string, Lazy<bool>> RecoveredKits { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets libraries indexed by both system name and optional short name.
    /// </summary>
    internal Dictionary<string, Library> Libraries { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the library selected by the root configuration's system name.
    /// </summary>
    internal Library DefaultLibrary { get; private set; }

    /// <summary>
    /// Gets the optional root-level variant override, applicable only to the default library.
    /// </summary>
    internal string DefaultVariant { get; private set; }

    /// <summary>
    /// Gets the token that marks this generation stale when source files change.
    /// </summary>
    internal IChangeToken ChangeToken { get; private set; }

    /// <summary>
    /// Groups a manifest with independently deferred mappings and search metadata.
    /// </summary>
    internal sealed class Library
    {
        private readonly Lazy<Dictionary<string, IconMapping>> _mapping;
        private readonly Lazy<Dictionary<string, string[]>> _tags;

        /// <summary>
        /// Registers deferred JSON reads without opening either file.
        /// </summary>
        /// <param name="files">The application data file provider.</param>
        /// <param name="root">The provider-relative library directory.</param>
        internal Library(IFileProvider files, string root)
        {
            _mapping = new(() => LoadMapping(files, root));
            _tags = new(() => LoadTags(files, root));
        }

        /// <summary>
        /// The owned immutable manifest, including the derived system name.
        /// </summary>
        internal IconLibrary Manifest;

        /// <summary>
        /// Gets conceptual mappings, loaded once on the first lookup or search.
        /// </summary>
        internal Dictionary<string, IconMapping> Mapping => _mapping.Value;

        /// <summary>
        /// Gets supplemental search terms, loaded only when icon metadata is requested.
        /// </summary>
        internal Dictionary<string, string[]> Tags => _tags.Value;

        /// <summary>
        /// Indexes variants by full name and optional short name.
        /// </summary>
        internal Dictionary<string, Variant> Variants { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Loads an archive index only when needed and fingerprints only requested overrides.
    /// </summary>
    internal sealed class Variant
    {
        private readonly IFileProvider _files;
        private readonly string _root;
        private readonly string _manifestRevision;
        private readonly Lazy<Dictionary<string, uint>> _archive;
        private readonly Lazy<FrozenSet<string>> _names;
        private readonly ConcurrentDictionary<string, Lazy<Source>> _sources = new(StringComparer.Ordinal);

        /// <summary>
        /// Creates a variant without reading its archive or enumerating overrides.
        /// </summary>
        /// <param name="files">The application data file provider.</param>
        /// <param name="root">The provider-relative variant directory.</param>
        /// <param name="manifest">The immutable variant settings.</param>
        /// <param name="manifestRevision">The content hash of library.json.</param>
        internal Variant(IFileProvider files, string root, IconVariant manifest, string manifestRevision)
        {
            _files = files;
            _root = root;
            Manifest = manifest;
            _manifestRevision = manifestRevision;
            _archive = new(LoadArchive);
            // Cache only names, not file objects or SVG bodies. A new catalog generation
            // gets a fresh set after the watcher reports changes to any source layer.
            _names = new(() => GetOverrideNames("user")
                .Concat(GetOverrideNames("icons"))
                .Concat(_archive.Value.Keys)
                .ToFrozenSet(StringComparer.Ordinal));
        }

        /// <summary>
        /// Gets the native grid and paint settings for this variant.
        /// </summary>
        internal IconVariant Manifest { get; }

        /// <summary>
        /// Gets all available names, indexed lazily once per catalog generation without reading SVG contents.
        /// </summary>
        internal FrozenSet<string> Names => _names.Value;

        /// <summary>
        /// Resolves and fingerprints a requested source once per catalog generation.
        /// </summary>
        /// <param name="name">The exact, validated icon name.</param>
        /// <returns>The source descriptor, or null when the icon does not exist.</returns>
        internal Source GetSource(string name)
        {
            // Lazy coalesces concurrent first requests. Missing names are not retained: arbitrary
            // user input must not grow a permanent negative-lookup cache.
            var source = _sources.GetOrAdd(name, static (key, variant) => new Lazy<Source>(() => variant.LoadSource(key)), this).Value;
            if (source == null)
            {
                _sources.TryRemove(name, out _);
            }

            return source;
        }

        /// <summary>
        /// Opens only the requested source and prepares its payload on a cache miss.
        /// </summary>
        /// <param name="info">The resolved icon identity within this variant.</param>
        /// <param name="source">The source descriptor established during lookup.</param>
        /// <param name="revision">The complete cache revision, including the preparation version.</param>
        /// <returns>The prepared payload, with all source handles closed.</returns>
        internal IconSvg Prepare(IconInfo info, Source source, string revision)
        {
            using var reader = OpenReader();
            return reader.Prepare(info, source, revision);
        }

        /// <summary>
        /// Creates a short-lived reader that reuses one archive for a sequential batch.
        /// No file is opened until the first source is requested.
        /// </summary>
        internal SourceReader OpenReader() => new(this);

        /// <summary>
        /// Owns source handles for one preparation operation, never for a catalog or cache lifetime.
        /// ZIP metadata is read once; individual SVG streams are closed after each parse.
        /// This reader is deliberately not shared between concurrent operations.
        /// </summary>
        internal sealed class SourceReader : IDisposable
        {
            private readonly Variant _variant;
            private ZipArchive _zip;

            /// <summary>
            /// Binds the reader to one variant without opening its archive.
            /// </summary>
            /// <param name="variant">The selected variant and its source provider.</param>
            internal SourceReader(Variant variant)
            {
                _variant = variant;
            }

            /// <summary>
            /// Prepares one source, retaining only ZIP metadata between calls.
            /// </summary>
            /// <param name="info">The resolved icon identity.</param>
            /// <param name="source">The source descriptor established during lookup.</param>
            /// <param name="revision">The prepared payload revision.</param>
            internal IconSvg Prepare(IconInfo info, Source source, string revision)
            {
                if (source.OverrideHash != null)
                {
                    using var stream = _variant._files.GetFileInfo(source.Path).CreateReadStream();
                    RequireSeekable(stream);
                    if (HashSource(stream, IconSvgParser.MaxLength) != source.OverrideHash)
                    {
                        throw new IOException("Icon override changed during lookup. Retry after catalog reload.");
                    }

                    stream.Position = 0;
                    return IconSvgParser.Parse(stream, info, _variant.Manifest, revision);
                }

                if (_zip == null)
                {
                    var stream = _variant._files.GetFileInfo(source.Path).CreateReadStream();
                    try
                    {
                        RequireSeekable(stream);
                        _zip = new ZipArchive(stream, ZipArchiveMode.Read);
                    }
                    catch
                    {
                        stream.Dispose();
                        throw;
                    }
                }

                var entry = _zip.GetEntry(info.Name + ".svg");
                if (entry == null || entry.Crc32 != source.Checksum || entry.Length > IconSvgParser.MaxLength)
                {
                    throw new IOException("Icon archive changed during lookup. Retry after catalog reload.");
                }

                using var svgStream = entry.Open();
                return IconSvgParser.Parse(svgStream, info, _variant.Manifest, revision);
            }

            /// <summary>
            /// Closes the archive and its underlying stream, including after a failed preparation.
            /// </summary>
            public void Dispose() => _zip?.Dispose();
        }

        /// <summary>
        /// Resolves user customizations, then system overrides, then the original archive.
        /// </summary>
        /// <param name="name">The exact, validated icon name.</param>
        private Source LoadSource(string name)
        {
            // Stop at the first existing source: lower layers must not incur file reads or hashing.
            var source = LoadOverride(name, "user") ?? LoadOverride(name, "icons");
            if (source != null)
            {
                return source;
            }

            return _archive.Value.TryGetValue(name, out var checksum)
                ? new Source(_root + "/icons.zip", _manifestRevision + ":zip:" + checksum.ToString("x8"), null, checksum)
                : null;
        }

        /// <summary>
        /// Fingerprints a requested loose SVG without retaining its file object or contents.
        /// </summary>
        /// <param name="name">The exact, validated icon name.</param>
        /// <param name="layer">The variant-relative user or icons directory.</param>
        /// <returns>The selected source, or null when this layer has no matching file.</returns>
        private Source LoadOverride(string name, string layer)
        {
            var path = _root + "/" + layer + "/" + name + ".svg";
            var file = _files.GetFileInfo(path);
            if (!file.Exists)
            {
                return null;
            }

            using var stream = file.CreateReadStream();
            var hash = HashSource(stream, IconSvgParser.MaxLength);
            // Include the layer so moving an identical file also changes the source revision.
            return new Source(path, _manifestRevision + ":" + layer + ":" + hash, hash, 0);
        }

        /// <summary>
        /// Reads only central-directory metadata, never SVG bodies or the entire archive for hashing.
        /// </summary>
        private Dictionary<string, uint> LoadArchive()
        {
            var result = new Dictionary<string, uint>(StringComparer.Ordinal);
            var file = _files.GetFileInfo(_root + "/icons.zip");
            if (!file.Exists)
            {
                // Libraries may consist entirely of loose SVGs; an absent archive is an empty layer.
                return result;
            }

            using var stream = file.CreateReadStream();
            RequireSeekable(stream);
            CheckLength(stream, 256 * 1024 * 1024);
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var totalLength = 0L;
            foreach (var entry in zip.Entries)
            {
                if (!entry.FullName.EndsWith(".svg", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = entry.FullName[..^4];
                if (!IconAddress.IsName(name) || entry.Length > IconSvgParser.MaxLength || !result.TryAdd(name, entry.Crc32))
                {
                    throw new InvalidDataException($"Invalid or duplicate SVG entry '{entry.FullName}' in {_root}/icons.zip.");
                }

                totalLength += entry.Length;
                if (totalLength > 256 * 1024 * 1024)
                {
                    throw new InvalidDataException($"Uncompressed icon archive exceeds the supported size limit: {_root}/icons.zip.");
                }
            }

            return result;
        }

        /// <summary>
        /// Enumerates custom filenames directly from the provider without caching the directory.
        /// </summary>
        /// <param name="layer">The variant-relative user or icons directory.</param>
        private IEnumerable<string> GetOverrideNames(string layer)
        {
            foreach (var file in _files.GetDirectoryContents(_root + "/" + layer))
            {
                if (!file.IsDirectory && file.Name.EndsWith(".svg", StringComparison.Ordinal))
                {
                    var name = file.Name[..^4];
                    if (!IconAddress.IsName(name))
                    {
                        throw new InvalidDataException($"Invalid override name '{file.Name}'.");
                    }

                    yield return name;
                }
            }
        }
    }

    /// <summary>
    /// Describes one requested source without owning file objects or content buffers.
    /// </summary>
    /// <param name="Path">The provider-relative source path.</param>
    /// <param name="Revision">The manifest and source fingerprint.</param>
    /// <param name="OverrideHash">The override content hash, or null for a ZIP entry.</param>
    /// <param name="Checksum">The ZIP entry checksum, unused for overrides.</param>
    internal sealed record Source(string Path, string Revision, string OverrideHash, uint Checksum);

    /// <summary>
    /// Loads configuration and library manifests, deferring icon sources and supplementary JSON.
    /// </summary>
    /// <param name="files">The application data file provider.</param>
    /// <returns>A manifest snapshot with its change token established before source reads.</returns>
    internal static IconCatalog Load(IFileProvider files)
    {
        // Watch before reading so a change during any deferred load invalidates this generation.
        // Watch source locations explicitly: generated files in App_Data/.cache/IconKits must never
        // invalidate their own catalog. Wildcards still discover new libraries and variants.
        var catalog = new IconCatalog
        {
            ChangeToken = new CompositeChangeToken(new[]
            {
                files.Watch("Icons/config.json"),
                files.Watch("Icons/*/library.json"),
                files.Watch("Icons/*/mapping.json"),
                files.Watch("Icons/*/metadata.json"),
                files.Watch("Icons/*/*/icons.zip"),
                files.Watch("Icons/*/*/user/*.svg"),
                files.Watch("Icons/*/*/icons/*.svg")
            })
        };
        using var config = ReadJson(files, "Icons/config.json", true);
        foreach (var dir in files.GetDirectoryContents("Icons").Where(x => x.IsDirectory))
        {
            var library = LoadLibrary(files, dir.Name);
            if (library == null)
            {
                continue;
            }

            foreach (var key in new[] { library.Manifest.SystemName, library.Manifest.ShortName }.Where(x => x != null).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!catalog.Libraries.TryAdd(key, library))
                {
                    throw new InvalidDataException($"Duplicate or ambiguous icon library name/short name '{key}'.");
                }
            }
        }

        var defaultName = config.RootElement.GetProperty("defaultLibrary").GetString();
        // Root defaults use a stable directory name, not a customizable short name.
        if (defaultName == null || !catalog.Libraries.TryGetValue(defaultName, out var defaultLibrary)
            || !defaultLibrary.Manifest.SystemName.Equals(defaultName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Icons/config.json must select an existing library by its system name.");
        }

        catalog.DefaultLibrary = defaultLibrary;
        catalog.DefaultVariant = config.RootElement.TryGetProperty("defaultVariant", out var defaultVariant)
            ? defaultVariant.GetString() : null;
        if (catalog.DefaultVariant != null && !defaultLibrary.Variants.ContainsKey(catalog.DefaultVariant))
        {
            throw new InvalidDataException("Unknown defaultVariant in Icons/config.json.");
        }

        if (config.RootElement.TryGetProperty("kits", out var kits))
        {
            foreach (var kit in kits.EnumerateObject())
            {
                if (!IconAddress.IsQualifier(kit.Name))
                {
                    throw new InvalidDataException($"Invalid icon kit name '{kit.Name}'.");
                }

                // Retain the array shorthand for kits that inherit all defaults.
                var definition = kit.Value;
                var isObject = definition.ValueKind == JsonValueKind.Object;
                var libraryName = isObject && definition.TryGetProperty("defaultLibrary", out var kitLibrary)
                    ? kitLibrary.GetString() : null;
                var variantName = isObject && definition.TryGetProperty("defaultVariant", out var kitVariant)
                    ? kitVariant.GetString() : null;
                var selectedLibrary = libraryName == null ? catalog.DefaultLibrary : catalog.Libraries.GetValueOrDefault(libraryName);
                if (selectedLibrary == null || libraryName != null
                    && !selectedLibrary.Manifest.SystemName.Equals(libraryName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Unknown defaultLibrary in icon kit '{kit.Name}'. Use the library system name.");
                }

                if (variantName != null && !selectedLibrary.Variants.ContainsKey(variantName))
                {
                    throw new InvalidDataException($"Unknown defaultVariant '{variantName}' in icon kit '{kit.Name}' for library '{selectedLibrary.Manifest.SystemName}'.");
                }

                var names = (isObject ? definition.GetProperty("icons") : definition)
                    .EnumerateArray().Select(x => x.GetString()).ToArray();
                if (names.Any(x => !IconAddress.IsName(x)) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
                {
                    throw new InvalidDataException($"Invalid or duplicate concepts in icon kit '{kit.Name}'.");
                }

                if (!catalog.Kits.TryAdd(kit.Name, new IconKit(kit.Name, names, libraryName, variantName)))
                {
                    throw new InvalidDataException($"Duplicate icon kit '{kit.Name}'.");
                }
            }
        }

        foreach (var kit in catalog.Kits.Values.OrderBy(x => x.Name == "shared" ? 0 : 1).ThenBy(x => x.Name, StringComparer.Ordinal))
        {
            foreach (var concept in kit.Icons)
            {
                catalog.ConceptKits.TryAdd(concept, kit);
            }
        }

        return catalog;
    }

    /// <summary>
    /// Reads and validates one manifest and registers its deferred variants.
    /// </summary>
    /// <param name="files">The application data file provider.</param>
    /// <param name="name">The library directory name.</param>
    private static Library LoadLibrary(IFileProvider files, string name)
    {
        var root = "Icons/" + name;
        var file = files.GetFileInfo(root + "/library.json");
        if (!file.Exists)
        {
            return null;
        }

        using var stream = file.CreateReadStream();
        RequireSeekable(stream);
        var revision = HashSource(stream, 1024 * 1024);
        stream.Position = 0;
        var manifest = JsonSerializer.Deserialize<IconLibrary>(stream, SmartJsonOptions.CamelCased)
            ?? throw new InvalidDataException($"Empty manifest: {root}/library.json.");
        // Identity comes from the containing directory, not a user-supplied JSON field.
        // Short names share the lookup namespace with full names (see registration below).
        if (!IconAddress.IsQualifier(name) || manifest.ShortName != null && !IconAddress.IsQualifier(manifest.ShortName) || manifest.Variants == null)
        {
            throw new InvalidDataException($"Invalid library manifest: {root}/library.json.");
        }

        // Complete derived identities once, before publication. Record initialization
        // and the frozen variant dictionary make all returned manifests safe to share.
        manifest = manifest with
        {
            SystemName = name,
            ShortName = manifest.ShortName?.ToLowerInvariant(),
            Variants = manifest.Variants.ToDictionary(x => x.Key, x =>
                x.Value == null
                    ? throw new InvalidDataException($"Invalid variant '{x.Key}' in {root}.")
                    : x.Value with { Name = x.Key, ShortName = x.Value.ShortName?.ToLowerInvariant() },
                StringComparer.OrdinalIgnoreCase)
        };
        var library = new Library(files, root) { Manifest = manifest };
        foreach (var pair in manifest.Variants)
        {
            if (!IconAddress.IsQualifier(pair.Key) || pair.Value == null
                || pair.Value.ShortName != null && !IconAddress.IsQualifier(pair.Value.ShortName)
                || (!double.IsFinite(pair.Value.StrokeWidthScale) || pair.Value.StrokeWidthScale < 0)
                || pair.Value.Stroke != null && (!System.Text.RegularExpressions.Regex.IsMatch(pair.Value.Stroke, @"^[A-Za-z0-9#.,% ()+-]+$")
                    || pair.Value.Stroke.Contains("url", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException($"Invalid variant '{pair.Key}' in {root}.");
            }

            var variant = new Variant(files, root + "/" + pair.Key, pair.Value, revision);
            foreach (var key in new[] { pair.Value.Name, pair.Value.ShortName }.Where(x => x != null).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                // Both selectors refer to the same object. Reject collisions rather than
                // letting directory enumeration order decide which variant wins.
                if (!library.Variants.TryAdd(key, variant))
                {
                    throw new InvalidDataException($"Duplicate or ambiguous variant name/short name '{key}' in {root}.");
                }
            }
        }

        if (!library.Variants.ContainsKey(manifest.DefaultVariant ?? string.Empty))
        {
            throw new InvalidDataException($"Unknown default variant in {root}/library.json.");
        }

        return library;
    }

    /// <summary>
    /// Reads conceptual names on the first lookup for a library.
    /// </summary>
    /// <param name="files">The application data file provider.</param>
    /// <param name="root">The provider-relative library directory.</param>
    private static Dictionary<string, IconMapping> LoadMapping(IFileProvider files, string root)
    {
        var result = new Dictionary<string, IconMapping>(StringComparer.Ordinal);
        using var mapping = ReadJson(files, root + "/mapping.json", false);
        // Mapping targets are icon names within this library, not addresses. Selecting
        // another library or variant remains the caller's responsibility.
        if (mapping != null)
        {
            foreach (var property in mapping.RootElement.EnumerateObject())
            {
                var target = property.Value.GetString();
                if (!IconAddress.IsName(property.Name))
                {
                    throw new InvalidDataException($"Mappings must contain unqualified icon names: {root}/mapping.json.");
                }

                result.Add(property.Name, IconMapping.Parse(target, $"{root}/mapping.json ({property.Name})"));
            }
        }

        return result;
    }

    /// <summary>
    /// Reads supplemental search terms only when metadata is requested.
    /// </summary>
    /// <param name="files">The application data file provider.</param>
    /// <param name="root">The provider-relative library directory.</param>
    private static Dictionary<string, string[]> LoadTags(IFileProvider files, string root)
    {
        var result = new Dictionary<string, string[]>(StringComparer.Ordinal);
        using var metadata = ReadJson(files, root + "/metadata.json", false);
        // Metadata enriches search only. Availability comes from archives and overrides,
        // so an absent metadata entry must never hide an otherwise usable icon.
        if (metadata != null && metadata.RootElement.TryGetProperty("icons", out var icons))
        {
            foreach (var icon in icons.EnumerateObject())
            {
                var tags = icon.Value.TryGetProperty("tags", out var values)
                    ? values.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : [];
                result.Add(icon.Name, tags);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads a bounded JSON document from the source provider.
    /// </summary>
    /// <param name="files">The application data file provider.</param>
    /// <param name="path">The provider-relative path to the JSON file.</param>
    /// <param name="required">Whether a missing file should throw instead of returning null.</param>
    /// <returns>A document owned by the caller, or null for an absent optional file.</returns>
    private static JsonDocument ReadJson(IFileProvider files, string path, bool required)
    {
        var file = files.GetFileInfo(path);
        if (!file.Exists)
        {
            return required ? throw new FileNotFoundException("Required icon configuration is missing.", path) : null;
        }

        using var stream = file.CreateReadStream();
        RequireSeekable(stream);
        CheckLength(stream, 16 * 1024 * 1024);
        return JsonDocument.Parse(stream);
    }

    /// <summary>
    /// Hashes source content with a fixed-size buffer without retaining its bytes.
    /// </summary>
    /// <param name="stream">The source stream, left open and advanced to its end.</param>
    /// <param name="limit">The maximum accepted source size in bytes.</param>
    /// <returns>The hexadecimal SHA-256 content fingerprint.</returns>
    private static string HashSource(Stream stream, long limit)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            var total = 0L;
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                total += count;
                if (total > limit)
                {
                    throw new InvalidDataException("Icon source exceeds the supported size limit.");
                }

                hash.AppendData(buffer, 0, count);
            }

            return Convert.ToHexStringLower(hash.GetHashAndReset());
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Rejects providers whose streams would force ZIP to buffer an entire archive internally.
    /// </summary>
    /// <param name="stream">The source stream supplied by the local file provider.</param>
    private static void RequireSeekable(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("Icon sources require seekable file streams.");
        }
    }

    /// <summary>
    /// Rejects oversized seekable sources before JSON or ZIP readers allocate their buffers.
    /// </summary>
    /// <param name="stream">The seekable source stream.</param>
    /// <param name="limit">The maximum supported byte length.</param>
    private static void CheckLength(Stream stream, long limit)
    {
        if (stream.Length > limit)
        {
            throw new InvalidDataException("Icon source exceeds the supported size limit.");
        }
    }
}
