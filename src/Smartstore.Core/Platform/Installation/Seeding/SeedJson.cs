using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Smartstore.Core.Installation.Seeding;

/// <summary>
/// Small helpers for working with seed JSON nodes. Field names are matched case-insensitively.
/// </summary>
internal static class SeedJson
{
    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static JsonNode Parse(string json, string source)
    {
        try
        {
            return JsonNode.Parse(json, documentOptions: DocumentOptions);
        }
        catch (JsonException ex)
        {
            throw new SeedDataFormatException($"Invalid JSON in '{source}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Finds the actual name of a field, ignoring case.
    /// </summary>
    public static string FindName(JsonObject obj, string name)
    {
        if (obj.ContainsKey(name))
        {
            return name;
        }

        foreach (var (key, _) in obj)
        {
            if (key.EqualsNoCase(name))
            {
                return key;
            }
        }

        return null;
    }

    public static bool TryGet(JsonObject obj, string name, out JsonNode value)
    {
        var actualName = FindName(obj, name);
        if (actualName == null)
        {
            value = null;
            return false;
        }

        value = obj[actualName];
        return true;
    }

    /// <summary>
    /// Removes a field (ignoring case) and returns its value.
    /// </summary>
    public static bool TryRemove(JsonObject obj, string name, out JsonNode value)
    {
        var actualName = FindName(obj, name);
        if (actualName == null)
        {
            value = null;
            return false;
        }

        value = obj[actualName];
        obj.Remove(actualName);
        return true;
    }

    /// <summary>
    /// Sets a field, replacing an existing field with the same name in any casing.
    /// </summary>
    public static void Set(JsonObject obj, string name, JsonNode value)
    {
        var actualName = FindName(obj, name) ?? name;
        obj[actualName] = value;
    }

    /// <summary>
    /// Gets the key of a record. Returns null if the key field is missing.
    /// </summary>
    public static string GetKey(JsonObject record, string keyField, string context)
    {
        if (!TryGet(record, keyField, out var node) || node == null)
        {
            return null;
        }

        return GetKeyValue(node, $"{context} → {keyField}");
    }

    /// <summary>
    /// Converts a key node (string or integer) to its string representation.
    /// </summary>
    public static string GetKeyValue(JsonNode node, string context)
    {
        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var str) && str.HasValue())
            {
                return str;
            }

            if (value.TryGetValue<long>(out var number))
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }
        }

        throw new SeedDataFormatException($"{context}: a key must be a non-empty string or an integer.");
    }

    public static JsonNode Clone(JsonNode node)
        => node?.DeepClone();
}
