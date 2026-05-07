#nullable enable

using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Html;
using Smartstore.Json;
using Smartstore.Utilities;

namespace Smartstore.Core.Widgets;

/// <summary>
/// Builds structured data (JSON-LD) for the current page by collecting fragments from various sources
/// (views, partials, components) and merging them into consolidated script blocks.
/// </summary>
public class JsonLdBuilder
{
    private readonly Dictionary<string, JsonObject> _fragments = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether any JSON-LD fragments have been registered.
    /// </summary>
    public bool HasFragments => _fragments.Count > 0;

    #region AddProperty overloads

    /// <summary>
    /// Adds a string property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
    /// <param name="key">The property name (e.g., "sku", "name").</param>
    /// <param name="value">The string value. Nulls are ignored.</param>
    public void AddProperty(string type, string key, string? value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        if (value == null)
        {
            return;
        }

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds an integer property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, int value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a long integer property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, long value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a decimal property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, decimal value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a double property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, double value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a boolean property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, bool value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a DateTime property to a JSON-LD fragment (serialized as ISO 8601).
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, DateTime value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    /// <summary>
    /// Adds a URI property to a JSON-LD fragment.
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    public void AddProperty(string type, string key, Uri? value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        if (value == null)
        {
            return;
        }

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value.ToString()));
    }

    /// <summary>
    /// Adds an arbitrary property to a JSON-LD fragment (fallback for unsupported types).
    /// First-write wins if the key already exists at the same level.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product", "BreadcrumbList").</param>
    /// <param name="key">The property name (e.g., "sku", "name").</param>
    /// <param name="value">The property value. Nulls are ignored.</param>
    public void AddProperty(string type, string key, object? value)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);

        if (value == null)
        {
            return;
        }

        var fragment = GetOrCreateFragment(type);
        SetOrMerge(fragment, key, JsonValue.Create(value));
    }

    #endregion

    /// <summary>
    /// Adds a nested object to a JSON-LD fragment.
    /// Deep-merges if the key already exists and both the existing and new values are objects.
    /// First-write wins for primitive conflicts within the merge.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product").</param>
    /// <param name="key">The property name (e.g., "offers", "aggregateRating").</param>
    /// <param name="obj">An anonymous object or POCO to be serialized as JSON.</param>
    public void AddObject(string type, string key, object obj)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);
        Guard.NotNull(obj);

        var fragment = GetOrCreateFragment(type);
        var jsonObj = JsonSerializer.SerializeToNode(obj)?.AsObject();

        if (jsonObj != null)
        {
            NormalizeTypeProperty(jsonObj);
            SetOrMerge(fragment, key, jsonObj);
        }
    }

    /// <summary>
    /// Adds an array to a JSON-LD fragment.
    /// First-write wins if the key already exists.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Product").</param>
    /// <param name="key">The property name (e.g., "image", "additionalProperty").</param>
    /// <param name="items">The enumerable items to serialize as a JSON array.</param>
    public void AddArray(string type, string key, IEnumerable items)
    {
        Guard.NotEmpty(type);
        Guard.NotEmpty(key);
        Guard.NotNull(items);

        var fragment = GetOrCreateFragment(type);

        // Arrays: first-write wins (no append, as merge semantics would be unpredictable)
        if (!fragment.ContainsKey(key))
        {
            var jsonArray = new JsonArray();
            foreach (var item in items)
            {
                if (item is string str)
                {
                    jsonArray.Add(JsonValue.Create(str));
                }
                else if (item != null)
                {
                    jsonArray.Add(JsonSerializer.SerializeToNode(item));
                }
            }
            fragment[key] = jsonArray;
        }
    }

    /// <summary>
    /// Renders all collected JSON-LD fragments as &lt;script type="application/ld+json"&gt; blocks.
    /// Each distinct @type is rendered as a separate script block with "@context": "https://schema.org/".
    /// </summary>
    /// <returns>HTML content containing all JSON-LD script tags.</returns>
    public IHtmlContent RenderScripts()
    {
        if (_fragments.Count == 0)
        {
            return HtmlString.Empty;
        }

        using var psb = StringBuilderPool.Instance.Get(out var sb);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        foreach (var (type, fragment) in _fragments)
        {
            // Ensure @context is present
            if (!fragment.ContainsKey("@context"))
            {
                fragment["@context"] = "https://schema.org/";
            }

            sb.Append("<script type=\"application/ld+json\">");
            sb.Append(JsonSerializer.Serialize(fragment, options /* SmartJsonOptions.Default */));
            sb.Append("</script>");
        }

        return new HtmlString(sb.ToString());
    }

    /// <summary>
    /// Sets or deep-merges a value into the fragment.
    /// - Primitives/Arrays: first-write wins.
    /// - Objects: recursive deep merge (nested properties are combined).
    /// </summary>
    private static void SetOrMerge(JsonObject target, string key, JsonNode? value)
    {
        if (!target.ContainsKey(key))
        {
            // Key does not exist yet → simply set
            target[key] = value;
        }
        else if (target[key] is JsonObject existingObj && value is JsonObject newObj)
        {
            // Both are objects → deep merge recursively
            DeepMerge(existingObj, newObj);
        }
        // else: Key exists and is not an object → first-write wins, do nothing
    }

    /// <summary>
    /// Deep merges <paramref name="source"/> into <paramref name="target"/>.
    /// Existing keys in target win for primitives/arrays.
    /// Nested objects are merged recursively.
    /// </summary>
    private static void DeepMerge(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (!target.ContainsKey(key))
            {
                // New key → adopt
                target[key] = value?.DeepClone();
            }
            else if (target[key] is JsonObject existingNested && value is JsonObject newNested)
            {
                // Both nested objects → recurse
                DeepMerge(existingNested, newNested);
            }
            // else: Key exists but is not an object → first-write wins
        }
    }

    private JsonObject GetOrCreateFragment(string type)
    {
        if (!_fragments.TryGetValue(type, out var fragment))
        {
            fragment = new JsonObject
            {
                ["@type"] = type
            };
            _fragments[type] = fragment;
        }
        return fragment;
    }

    /// <summary>
    /// Recursively normalizes "type" properties to "@type" for Schema.org compatibility.
    /// C# anonymous objects cannot define properties starting with "@", so we rename them post-serialization.
    /// </summary>
    private static void NormalizeTypeProperty(JsonObject obj)
    {
        if (obj.ContainsKey("type"))
        {
            var typeValue = obj["type"];
            obj.Remove("type");
            obj["@type"] = typeValue;
        }

        // Recurse into nested objects
        foreach (var (key, value) in obj.ToList())
        {
            if (value is JsonObject nested)
            {
                NormalizeTypeProperty(nested);
            }
            else if (value is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is JsonObject nestedItem)
                    {
                        NormalizeTypeProperty(nestedItem);
                    }
                }
            }
        }
    }
}
