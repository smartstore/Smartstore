using System.Text.Json.Nodes;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Merges seed records with defaults and culture overlays. Pure JSON logic without any file access.
/// </summary>
/// <remarks>
/// An overlay for a collection is an object. Its fields are either record keys (a partial record that is
/// merged into the existing record) or directives:
/// <list type="bullet">
///     <item><c>$all</c>: partial record merged into every existing record before keyed patches.</item>
///     <item><c>$remove</c>: array of keys of records to remove.</item>
///     <item><c>$add</c>: array of complete new records, appended after all other directives.</item>
///     <item><c>$mode</c>: "merge" (default) or "replace". "replace" requires <c>$items</c> and drops all base records.</item>
///     <item><c>$items</c>: array of complete records that replace the base records (only with <c>$mode: "replace"</c>).</item>
///     <item><c>$aliases</c>: object mapping keys to other keys, for reference resolution (top level only).</item>
/// </list>
/// Within a record, nested objects are merged field by field. Declared child collections accept an overlay
/// object (merged by child key with the same directives) or an array (replaces the whole collection).
/// All other values, including arrays, replace the existing value.
/// </remarks>
public static class SeedJsonMerger
{
    public const string AllDirective = "$all";
    public const string RemoveDirective = "$remove";
    public const string AddDirective = "$add";
    public const string ModeDirective = "$mode";
    public const string ItemsDirective = "$items";
    public const string AliasesDirective = "$aliases";

    private static readonly HashSet<string> KnownDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        AllDirective, RemoveDirective, AddDirective, ModeDirective, ItemsDirective, AliasesDirective
    };

    /// <summary>
    /// Applies <paramref name="defaults"/> to a record: values of the record win.
    /// </summary>
    /// <returns>A new record instance.</returns>
    public static JsonObject ApplyDefaults(JsonObject defaults, JsonObject record, SeedCollectionSpec spec, string context)
    {
        Guard.NotNull(record);

        if (defaults == null)
        {
            return (JsonObject)record.DeepClone();
        }

        var result = (JsonObject)defaults.DeepClone();

        // Child collections of a record replace the defaults' collections instead of patching them.
        foreach (var (name, value) in record)
        {
            if (value is JsonObject obj && (spec == null || !spec.Children.ContainsKey(name)) && SeedJson.TryGet(result, name, out var existing) && existing is JsonObject existingObj)
            {
                MergeRecord(existingObj, obj, null, $"{context} → {name}");
            }
            else
            {
                SeedJson.Set(result, name, SeedJson.Clone(value));
            }
        }

        return result;
    }

    /// <summary>
    /// Applies a collection overlay to records. The passed records are modified in place.
    /// </summary>
    /// <param name="records">Records to apply the overlay to.</param>
    /// <param name="overlay">The overlay object. If null, the records are returned unchanged.</param>
    /// <param name="spec">Key and child collections of the records.</param>
    /// <param name="context">Describes the location for error messages, e.g. the file path.</param>
    /// <param name="aliases">
    /// Receives aliases declared by <c>$aliases</c>. If null, <c>$aliases</c> is not allowed (nested collections).
    /// </param>
    /// <returns>The resulting records in order: remaining base records followed by added records.</returns>
    public static List<JsonObject> ApplyOverlay(
        IEnumerable<JsonObject> records,
        JsonObject overlay,
        SeedCollectionSpec spec,
        string context,
        IDictionary<string, string> aliases = null)
    {
        Guard.NotNull(records);
        Guard.NotNull(spec);

        var result = records.ToList();

        if (overlay == null)
        {
            return result;
        }

        foreach (var (name, _) in overlay)
        {
            if (name.StartsWith('$') && !KnownDirectives.Contains(name))
            {
                throw new SeedDataFormatException($"{context}: unknown directive '{name}'.");
            }
        }

        if (SeedJson.TryGet(overlay, AliasesDirective, out var aliasesNode))
        {
            ReadAliases(aliasesNode, aliases, context);
        }

        var mode = SeedJson.TryGet(overlay, ModeDirective, out var modeNode) ? modeNode?.GetValue<string>() : "merge";

        if (mode.EqualsNoCase("replace"))
        {
            return Replace(overlay, spec, context);
        }

        if (!mode.EqualsNoCase("merge"))
        {
            throw new SeedDataFormatException($"{context}: unknown {ModeDirective} '{mode}'. Use 'merge' or 'replace'.");
        }

        if (SeedJson.TryGet(overlay, ItemsDirective, out _))
        {
            throw new SeedDataFormatException($"{context}: {ItemsDirective} requires {ModeDirective}: \"replace\".");
        }

        var index = BuildIndex(result, spec, context);

        // 1. $remove
        if (SeedJson.TryGet(overlay, RemoveDirective, out var removeNode))
        {
            if (removeNode is not JsonArray removeKeys)
            {
                throw new SeedDataFormatException($"{context}: {RemoveDirective} must be an array of keys.");
            }

            foreach (var keyNode in removeKeys)
            {
                var key = SeedJson.GetKeyValue(keyNode, $"{context} → {RemoveDirective}");
                if (!index.Remove(key, out var record))
                {
                    throw new SeedDataFormatException($"{context}: cannot remove unknown key '{key}'.");
                }

                result.Remove(record);
            }
        }

        // 2. $all
        if (SeedJson.TryGet(overlay, AllDirective, out var allNode))
        {
            if (allNode is not JsonObject allPatch)
            {
                throw new SeedDataFormatException($"{context}: {AllDirective} must be an object.");
            }

            foreach (var record in result)
            {
                MergeRecord(record, allPatch, spec, $"{context} → {AllDirective}");
            }
        }

        // 3. Keyed patches
        foreach (var (key, patchNode) in overlay)
        {
            if (key.StartsWith('$'))
            {
                continue;
            }

            if (!index.TryGetValue(key, out var record))
            {
                throw new SeedDataFormatException($"{context}: unknown key '{key}'. Use {AddDirective} to add new records.");
            }

            if (patchNode is not JsonObject patch)
            {
                throw new SeedDataFormatException($"{context} → {key}: a patch must be an object.");
            }

            MergeRecord(record, patch, spec, $"{context} → {key}");
        }

        // 4. $add
        if (SeedJson.TryGet(overlay, AddDirective, out var addNode))
        {
            foreach (var record in ReadRecords(addNode, $"{context} → {AddDirective}").Select(x => (JsonObject)x.DeepClone()))
            {
                var key = SeedJson.GetKey(record, spec.KeyField, $"{context} → {AddDirective}")
                    ?? throw new SeedDataFormatException($"{context} → {AddDirective}: record without key field '{spec.KeyField}'.");

                if (!index.TryAdd(key, record))
                {
                    throw new SeedDataFormatException($"{context} → {AddDirective}: key '{key}' already exists. Patch it instead.");
                }

                result.Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// Merges <paramref name="patch"/> into <paramref name="target"/>.
    /// </summary>
    /// <param name="spec">Declares child collections. Null for plain nested objects.</param>
    public static void MergeRecord(JsonObject target, JsonObject patch, SeedCollectionSpec spec, string context)
    {
        Guard.NotNull(target);
        Guard.NotNull(patch);

        foreach (var (name, value) in patch)
        {
            if (name.StartsWith('$'))
            {
                throw new SeedDataFormatException($"{context}: directive '{name}' is only allowed at collection level.");
            }

            SeedJson.TryGet(target, name, out var existing);

            if (spec != null && spec.Children.TryGetValue(name, out var childSpec) && value is JsonObject childOverlay)
            {
                var childRecords = ReadRecords(existing ?? new JsonArray(), $"{context} → {name}");
                var merged = ApplyOverlay(childRecords, childOverlay, childSpec, $"{context} → {name}");

                // Detach merged nodes from their old parent before re-adding them.
                foreach (var child in merged)
                {
                    (child.Parent as JsonArray)?.Remove(child);
                }

                SeedJson.Set(target, name, new JsonArray([.. merged]));
            }
            else if (value is JsonObject obj && existing is JsonObject existingObj && (spec == null || !spec.Children.ContainsKey(name)))
            {
                MergeRecord(existingObj, obj, null, $"{context} → {name}");
            }
            else
            {
                SeedJson.Set(target, name, SeedJson.Clone(value));
            }
        }
    }

    /// <summary>
    /// Reads an array of record objects.
    /// </summary>
    internal static List<JsonObject> ReadRecords(JsonNode node, string context)
    {
        if (node is not JsonArray array)
        {
            throw new SeedDataFormatException($"{context}: expected an array of records.");
        }

        var records = new List<JsonObject>(array.Count);
        foreach (var item in array)
        {
            if (item is not JsonObject obj)
            {
                throw new SeedDataFormatException($"{context}: expected an array of records, but found '{item?.GetValueKind()}'.");
            }

            records.Add(obj);
        }

        return records;
    }

    private static List<JsonObject> Replace(JsonObject overlay, SeedCollectionSpec spec, string context)
    {
        foreach (var (name, _) in overlay)
        {
            if (!name.EqualsNoCase(ModeDirective) && !name.EqualsNoCase(ItemsDirective) && !name.EqualsNoCase(AliasesDirective))
            {
                throw new SeedDataFormatException($"{context}: '{name}' is not allowed with {ModeDirective}: \"replace\".");
            }
        }

        if (!SeedJson.TryGet(overlay, ItemsDirective, out var itemsNode))
        {
            throw new SeedDataFormatException($"{context}: {ModeDirective}: \"replace\" requires {ItemsDirective}.");
        }

        var records = ReadRecords(itemsNode, $"{context} → {ItemsDirective}")
            .Select(x => (JsonObject)x.DeepClone())
            .ToList();

        // Validates keys.
        BuildIndex(records, spec, $"{context} → {ItemsDirective}");

        return records;
    }

    private static Dictionary<string, JsonObject> BuildIndex(List<JsonObject> records, SeedCollectionSpec spec, string context)
    {
        var index = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            var key = SeedJson.GetKey(record, spec.KeyField, context)
                ?? throw new SeedDataFormatException($"{context}: record without key field '{spec.KeyField}'.");

            if (!index.TryAdd(key, record))
            {
                throw new SeedDataFormatException($"{context}: duplicate key '{key}'.");
            }
        }

        return index;
    }

    private static void ReadAliases(JsonNode node, IDictionary<string, string> aliases, string context)
    {
        if (aliases == null)
        {
            throw new SeedDataFormatException($"{context}: {AliasesDirective} is only allowed at file level.");
        }

        if (node is not JsonObject obj)
        {
            throw new SeedDataFormatException($"{context}: {AliasesDirective} must be an object.");
        }

        foreach (var (alias, target) in obj)
        {
            aliases[alias] = SeedJson.GetKeyValue(target, $"{context} → {AliasesDirective} → {alias}");
        }
    }
}
