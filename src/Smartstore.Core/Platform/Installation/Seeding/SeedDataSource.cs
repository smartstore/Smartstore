using System.Text.Json;
using System.Text.Json.Nodes;
using Smartstore.IO;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// A merged seed record.
/// </summary>
/// <param name="Key">The record key.</param>
/// <param name="Data">The merged record data (defaults, base record and culture overlay).</param>
/// <param name="Source">Subpath of the base file that declares the record (or of the overlay that added it).</param>
public sealed record SeedRecord(string Key, JsonObject Data, string Source);

/// <summary>
/// All merged records of a seed entity descriptor.
/// </summary>
public sealed class SeedRecordSet
{
    public SeedRecordSet(SeedEntityDescriptor descriptor, IReadOnlyList<SeedRecord> records, IReadOnlyDictionary<string, string> aliases)
    {
        Descriptor = descriptor;
        Records = records;
        Aliases = aliases;
    }

    public SeedEntityDescriptor Descriptor { get; }
    public IReadOnlyList<SeedRecord> Records { get; }

    /// <summary>
    /// Key aliases declared by culture overlays via <c>$aliases</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Aliases { get; }
}

/// <summary>
/// Metadata of an installable culture, read from "cultures/{culture}/culture.json".
/// </summary>
public sealed class SeedCulture
{
    /// <summary>
    /// The culture code, taken from the folder name, e.g. "de-DE".
    /// </summary>
    public string Culture { get; set; }
    public string Name { get; set; }
    public string UniqueSeoCode { get; set; }
    public string FlagImageFileName { get; set; }
}

/// <summary>
/// Reads seed records from the seed folder (usually "App_Data/Seed") and merges them with
/// defaults and the overlays of a culture.
/// </summary>
/// <remarks>
/// Folder layout:
/// <code>
/// base/{path}                   Base records (JSON arrays) and defaults files (JSON objects)
/// cultures/{culture}/{path}     Overlays mirroring the base paths, plus "culture.json"
/// media/                        Sample media files
/// </code>
/// A top-level string field with the value <c>"@file"</c> is replaced by the content of
/// "{jsonFileNameWithoutExtension}/{key}.{field}.html" next to the JSON file that sets it.
/// <c>"@file:{relativePath}"</c> names the file explicitly.
/// </remarks>
public sealed class SeedDataSource
{
    /// <summary>
    /// The seed root folder relative to "App_Data".
    /// </summary>
    public const string AppDataFolder = "Seed";
    public const string BaseFolder = "base";
    public const string CulturesFolder = "cultures";
    public const string MediaFolder = "media";
    public const string CultureFileName = "culture.json";

    /// <summary>
    /// Folder of a culture that holds its locale resource files ("*.smres.xml" and "head.txt").
    /// </summary>
    public const string ResourcesFolder = "resources";
    public const string FileDirective = "@file";

    private static readonly JsonSerializerOptions CultureJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly char[] InvalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    /// <param name="root">The seed root folder, e.g. "App_Data/Seed".</param>
    /// <param name="culture">The culture whose overlays to apply, or null for base data only.</param>
    public SeedDataSource(IFileSystem root, string culture)
    {
        Root = Guard.NotNull(root);
        Culture = culture.NullEmpty();
    }

    public IFileSystem Root { get; }

    public string Culture { get; }

    /// <summary>
    /// Discovers all cultures that have a "culture.json" file, ordered by culture code.
    /// </summary>
    public static IReadOnlyList<SeedCulture> DiscoverCultures(IFileSystem root)
    {
        Guard.NotNull(root);

        if (!root.DirectoryExists(CulturesFolder))
        {
            return [];
        }

        var result = new List<SeedCulture>();

        foreach (var dir in root.EnumerateDirectories(CulturesFolder).OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            var file = root.GetFile(PathUtility.Join(dir.SubPath, CultureFileName));
            if (!file.Exists)
            {
                continue;
            }

            SeedCulture culture;
            try
            {
                culture = JsonSerializer.Deserialize<SeedCulture>(file.ReadAllText(), CultureJsonOptions);
            }
            catch (JsonException ex)
            {
                throw new SeedDataFormatException($"Invalid JSON in '{PathOf(file)}': {ex.Message}", ex);
            }

            culture.Culture = dir.Name;
            result.Add(culture);
        }

        return result;
    }

    /// <summary>
    /// Loads and merges all records of <paramref name="descriptor"/>.
    /// </summary>
    public SeedRecordSet Load(SeedEntityDescriptor descriptor)
    {
        Guard.NotNull(descriptor);

        if (descriptor.Files.Count == 0)
        {
            throw new InvalidOperationException($"Seed descriptor '{descriptor.Name}' declares no files.");
        }

        var spec = descriptor.Spec;
        var defaults = LoadDefaults(descriptor);
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var records = new List<SeedRecord>();
        var keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var baseFile in ResolveBaseFiles(descriptor))
        {
            var source = PathOf(baseFile);
            var baseRecords = SeedJsonMerger.ReadRecords(ParseFile(baseFile), source);

            foreach (var record in baseRecords)
            {
                var key = SeedJson.GetKey(record, spec.KeyField, source)
                    ?? throw new SeedDataFormatException($"{source}: record without key field '{spec.KeyField}'.");

                ResolveFileReferences(record, key, baseFile);
            }

            var merged = baseRecords
                .Select(x => SeedJsonMerger.ApplyDefaults(defaults, x, spec, source))
                .ToList();

            var overlayFile = GetOverlayFile(baseFile);
            if (overlayFile != null)
            {
                var overlay = ParseOverlay(overlayFile, spec);
                merged = SeedJsonMerger.ApplyOverlay(merged, overlay, spec, PathOf(overlayFile), aliases);
            }

            foreach (var record in merged)
            {
                var key = SeedJson.GetKey(record, spec.KeyField, source);
                if (!keys.TryAdd(key, source))
                {
                    throw new SeedDataFormatException($"Duplicate {descriptor.Name} key '{key}' in '{source}' and '{keys[key]}'.");
                }

                EnsureNoFileDirectives(record, $"{source} → {key}");
                records.Add(new SeedRecord(key, record, source));
            }
        }

        return new SeedRecordSet(descriptor, records, aliases);
    }

    /// <summary>
    /// Loads an object file (e.g. "core/settings.json") and merges the culture overlay into it field by field.
    /// </summary>
    public JsonObject LoadObject(string relativePath)
    {
        Guard.NotEmpty(relativePath);

        var file = Root.GetFile(PathUtility.Join(BaseFolder, relativePath));
        if (!file.Exists)
        {
            throw new SeedDataFormatException($"Seed file '{PathOf(file)}' does not exist.");
        }

        if (ParseFile(file) is not JsonObject result)
        {
            throw new SeedDataFormatException($"{PathOf(file)}: an object file must contain an object.");
        }

        var overlayFile = GetOverlayFile(file);
        if (overlayFile != null)
        {
            if (ParseFile(overlayFile) is not JsonObject overlay)
            {
                throw new SeedDataFormatException($"{PathOf(overlayFile)}: an object overlay must contain an object.");
            }

            SeedJsonMerger.MergeRecord(result, overlay, null, PathOf(overlayFile));
        }

        return result;
    }

    /// <summary>
    /// Gets the overlay file of a culture for a base subpath, or null if there is none.
    /// </summary>
    public IFile GetOverlayFile(IFile baseFile)
    {
        if (Culture == null)
        {
            return null;
        }

        var relativePath = PathOf(baseFile)[(BaseFolder.Length + 1)..];
        var file = Root.GetFile(PathUtility.Join(CulturesFolder, Culture, relativePath));

        return file.Exists ? file : null;
    }

    private JsonObject LoadDefaults(SeedEntityDescriptor descriptor)
    {
        if (descriptor.DefaultsFile == null)
        {
            return null;
        }

        var file = Root.GetFile(PathUtility.Join(BaseFolder, descriptor.DefaultsFile));
        if (!file.Exists)
        {
            throw new SeedDataFormatException($"Defaults file '{PathOf(file)}' of '{descriptor.Name}' does not exist.");
        }

        if (ParseFile(file) is not JsonObject defaults)
        {
            throw new SeedDataFormatException($"{PathOf(file)}: a defaults file must contain an object.");
        }

        var overlayFile = GetOverlayFile(file);
        if (overlayFile != null)
        {
            if (ParseFile(overlayFile) is not JsonObject overlay)
            {
                throw new SeedDataFormatException($"{PathOf(overlayFile)}: a defaults overlay must contain an object.");
            }

            SeedJsonMerger.MergeRecord(defaults, overlay, descriptor.Spec, PathOf(overlayFile));
        }

        return defaults;
    }

    private IEnumerable<IFile> ResolveBaseFiles(SeedEntityDescriptor descriptor)
    {
        foreach (var pattern in descriptor.Files)
        {
            var subpath = PathUtility.Join(BaseFolder, pattern);
            var fileName = Path.GetFileName(subpath);

            if (fileName.IndexOfAny(['*', '?']) < 0)
            {
                var file = Root.GetFile(subpath);
                if (!file.Exists)
                {
                    throw new SeedDataFormatException($"Seed file '{subpath}' of '{descriptor.Name}' does not exist.");
                }

                yield return file;
                continue;
            }

            var dir = subpath[..^fileName.Length].TrimEnd('/');
            var files = Root.DirectoryExists(dir)
                ? Root.EnumerateFiles(dir, fileName).Where(x => !x.Name.StartsWith('_')).OrderBy(x => x.Name, StringComparer.Ordinal).ToList()
                : [];

            if (files.Count == 0)
            {
                throw new SeedDataFormatException($"No seed files match '{subpath}' of '{descriptor.Name}'.");
            }

            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    private JsonObject ParseOverlay(IFile overlayFile, SeedCollectionSpec spec)
    {
        if (ParseFile(overlayFile) is not JsonObject overlay)
        {
            throw new SeedDataFormatException($"{PathOf(overlayFile)}: an overlay must contain an object (keys and directives).");
        }

        // Resolve @file in keyed patches and added records, relative to the overlay file.
        foreach (var (name, value) in overlay)
        {
            if (name.EqualsNoCase(SeedJsonMerger.AddDirective) || name.EqualsNoCase(SeedJsonMerger.ItemsDirective))
            {
                foreach (var record in SeedJsonMerger.ReadRecords(value, $"{PathOf(overlayFile)} → {name}"))
                {
                    var key = SeedJson.GetKey(record, spec.KeyField, PathOf(overlayFile))
                        ?? throw new SeedDataFormatException($"{PathOf(overlayFile)} → {name}: record without key field '{spec.KeyField}'.");

                    ResolveFileReferences(record, key, overlayFile);
                }
            }
            else if (!name.StartsWith('$') && value is JsonObject patch)
            {
                ResolveFileReferences(patch, name, overlayFile);
            }
        }

        return overlay;
    }

    private void ResolveFileReferences(JsonObject record, string key, IFile jsonFile)
    {
        foreach (var (field, value) in record.ToList())
        {
            if (value is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var str) || !str.StartsWith(FileDirective, StringComparison.Ordinal))
            {
                continue;
            }

            string relativePath;
            if (str == FileDirective)
            {
                var safeKey = string.Join('_', key.Split(InvalidFileNameChars));
                relativePath = $"{jsonFile.NameWithoutExtension}/{safeKey}.{field}.html";
            }
            else if (str.StartsWith(FileDirective + ":", StringComparison.Ordinal))
            {
                relativePath = str[(FileDirective.Length + 1)..].Trim();
            }
            else
            {
                continue;
            }

            var file = Root.GetFile(PathUtility.Join(jsonFile.Directory, relativePath));
            if (!file.Exists)
            {
                throw new SeedDataFormatException($"{PathOf(jsonFile)} → {key} → {field}: file '{PathOf(file)}' does not exist.");
            }

            record[field] = file.ReadAllText();
        }
    }

    private static void EnsureNoFileDirectives(JsonNode node, string context)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, value) in obj)
                {
                    EnsureNoFileDirectives(value, $"{context} → {name}");
                }
                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    EnsureNoFileDirectives(item, context);
                }
                break;
            case JsonValue value when value.TryGetValue<string>(out var str) && str.StartsWith(FileDirective, StringComparison.Ordinal):
                throw new SeedDataFormatException($"{context}: '{FileDirective}' is only supported in top-level fields of records and patches.");
        }
    }

    private static JsonNode ParseFile(IFile file)
        => SeedJson.Parse(file.ReadAllText(), PathOf(file));

    private static string PathOf(IFile file)
        => file.SubPath.Replace('\\', '/');
}
