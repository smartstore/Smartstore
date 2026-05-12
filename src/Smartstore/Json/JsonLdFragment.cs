#nullable enable

using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Smartstore.Json.Converters;

namespace Smartstore.Json;

/// <summary>
/// Represents a single JSON-LD fragment for one schema.org @type.
/// Supports a fluent API for adding properties, nested objects, and arrays.
/// All methods use first-write-wins semantics for conflicting keys.
/// </summary>
public class JsonLdFragment
{
    private static readonly string[] _propertiesToNormalize = ["type", "id"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonLdFragmentConverter() }
    };

    private readonly JsonObject _data;

    // TODO: (jsonld) (prio low) Context url should be without ending / to be conform to spec.
    private JsonLdFragment(string type, bool withContext = false)
    {
        Type = type;
        _data = withContext
            ? new JsonObject { ["@context"] = "https://schema.org/", ["@type"] = type }
            : new JsonObject { ["@type"] = type };
    }

    /// <summary>
    /// Gets the @type identifier.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Explicitly converts this <see cref="JsonLdFragment"/> to its underlying <see cref="JsonObject"/>.
    /// Useful when a raw <see cref="JsonObject"/> is required, e.g. for <c>Obj(string key, JsonObject raw)</c>.
    /// </summary>
    public static explicit operator JsonObject(JsonLdFragment obj) 
        => obj._data;

    /// <summary>
    /// Creates a new top-level <see cref="JsonLdFragment"/> that includes <c>"@context": "https://schema.org/"</c>
    /// as the first property, followed by <c>"@type"</c>. Use this for root fragments rendered directly
    /// as JSON-LD script blocks.
    /// </summary>
    internal static JsonLdFragment CreateTopLevel(string type) 
        => new(type, withContext: true);

    /// <summary>
    /// Creates a new <see cref="JsonLdFragment"/> with the given schema.org @type
    /// and optionally merges the serialized properties of <paramref name="properties"/> into it.
    /// Nested <see cref="JsonLdFragment"/> values inside <paramref name="properties"/> are supported
    /// and will be serialized as their underlying JSON data.
    /// </summary>
    /// <param name="type">The schema.org @type (e.g., "Brand", "AggregateRating").</param>
    /// <param name="properties">
    /// An anonymous object or POCO whose properties are merged into the fragment.
    /// Nulls are ignored. Nested <see cref="JsonLdFragment"/> values are serialized correctly.
    /// </param>
    public static JsonLdFragment Create(string type, object? properties = null)
    {
        Guard.NotEmpty(type);

        var fragment = new JsonLdFragment(type);

        if (properties != null)
        {
            var jsonObj = JsonSerializer.SerializeToNode(properties, _jsonOptions)?.AsObject();
            if (jsonObj != null)
            {
                NormalizePropertyNames(jsonObj);
                foreach (var (key, value) in jsonObj)
                {
                    // @type is already set in the constructor; skip any accidental override
                    if (!fragment._data.ContainsKey(key))
                    {
                        fragment._data[key] = value?.DeepClone();
                    }
                }
            }
        }

        return fragment;
    }

    public JsonObject AsJsonObject() 
        => _data;

    #region Prop overloads

    public JsonLdFragment Prop(string key, string? value)
    {
        Guard.NotEmpty(key);
        if (value != null) SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, int value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, long value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, decimal value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, double value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, bool value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, DateTime value)
    {
        Guard.NotEmpty(key);
        SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    public JsonLdFragment Prop(string key, Uri? value)
    {
        Guard.NotEmpty(key);
        if (value != null) SetOrMerge(_data, key, JsonValue.Create(value.ToString()));
        return this;
    }

    /// <summary>
    /// Adds a property to this JSON-LD fragment (fallback overload for unsupported types).
    /// First-write wins if the key already exists. Nulls are ignored.
    /// </summary>
    public JsonLdFragment Prop(string key, object? value)
    {
        Guard.NotEmpty(key);
        if (value != null) SetOrMerge(_data, key, JsonValue.Create(value));
        return this;
    }

    #endregion

    /// <summary>
    /// Adds a typed nested object to this JSON-LD fragment.
    /// The <paramref name="type"/> is written as <c>"@type"</c> directly.
    /// Deep-merges if the key already exists.
    /// </summary>
    /// <summary>
    /// Merges all properties of <paramref name="properties"/> into this JSON-LD fragment.
    /// Nested <see cref="JsonLdFragment"/> values inside <paramref name="properties"/> are supported.
    /// </summary>
    /// <param name="properties">An anonymous object or POCO whose properties are merged into this fragment.</param>
    /// <param name="overwrite">
    /// When <see langword="false"/> (default), first-write-wins: existing primitive/array values are kept.
    /// When <see langword="true"/>, existing primitive/array values are overwritten; nested objects are always deep-merged.
    /// </param>
    public JsonLdFragment Merge(object properties, bool overwrite = false)
    {
        Guard.NotNull(properties);

        var jsonObj = JsonSerializer.SerializeToNode(properties, _jsonOptions)?.AsObject();
        if (jsonObj != null)
        {
            NormalizePropertyNames(jsonObj);
            MergeInto(_data, jsonObj, overwrite);
        }
        return this;
    }

    private static void MergeInto(JsonObject target, JsonObject source, bool overwrite)
    {
        foreach (var (key, value) in source)
        {
            if (key == "@type")
            {
                // @type is managed by the constructor; never overwrite
                continue;
            }

            if (!target.ContainsKey(key))
            {
                target[key] = value?.DeepClone();
            }
            else if (target[key] is JsonObject existingObj && value is JsonObject newObj)
            {
                // Always deep-merge nested objects regardless of overwrite flag
                MergeInto(existingObj, newObj, overwrite);
            }
            else if (overwrite)
            {
                target[key] = value?.DeepClone();
            }
            // else: first-write-wins, do nothing
        }
    }

    /// <param name="key">The property name to nest under (e.g., "offers", "brand").</param>
    /// <param name="type">The schema.org @type of the nested object (e.g., "Offer", "Brand").</param>
    /// <param name="properties">Optional additional properties as an anonymous object or POCO.</param>
    public JsonLdFragment Obj(string key, string type, object? properties = null)
    {
        Guard.NotEmpty(key);
        Guard.NotEmpty(type);

        SetOrMerge(_data, key, Create(type, properties).AsJsonObject());
        return this;
    }

    /// <summary>
    /// Embeds a pre-built <see cref="JsonLdFragment"/> as a nested value under <paramref name="key"/>.
    /// Deep-merges if the key already exists.
    /// </summary>
    public JsonLdFragment Obj(string key, JsonLdFragment nested)
    {
        Guard.NotEmpty(key);
        Guard.NotNull(nested);

        SetOrMerge(_data, key, nested.AsJsonObject().DeepClone());
        return this;
    }

    /// <summary>
    /// Embeds a raw <see cref="JsonObject"/> as a nested value under <paramref name="key"/>.
    /// Deep-merges if the key already exists.
    /// </summary>
    public JsonLdFragment Obj(string key, JsonObject raw)
    {
        Guard.NotEmpty(key);
        Guard.NotNull(raw);

        var clone = raw.DeepClone().AsObject();
        NormalizePropertyNames(clone);
        SetOrMerge(_data, key, clone);
        return this;
    }

    /// <summary>
    /// Adds an array to this JSON-LD fragment.
    /// </summary>
    public JsonLdFragment Arr(string key, IEnumerable items)
    {
        Guard.NotEmpty(key);
        Guard.NotNull(items);

        if (!_data.ContainsKey(key))
        {
            var jsonArray = new JsonArray();
            foreach (var item in items)
            {
                if (item is string str)
                {
                    jsonArray.Add(JsonValue.Create(str));
                }
                else if (item is JsonLdFragment ldObj)
                {
                    jsonArray.Add(((JsonObject)ldObj).DeepClone());
                }
                else if (item is JsonObject rawObj)
                {
                    jsonArray.Add(rawObj.DeepClone());
                }
                else if (item != null)
                {
                    var node = JsonSerializer.SerializeToNode(item, _jsonOptions);
                    if (node is JsonObject nodeObj) NormalizePropertyNames(nodeObj);
                    jsonArray.Add(node);
                }
            }
            _data[key] = jsonArray;
        }
        return this;
    }

    /// <summary>
    /// Recursively normalizes <c>"type"</c> to <c>"@type"</c> and <c>"id"</c> to <c>"@id"</c> 
    /// and moves them to index 0 so they appear before all other properties. C# anonymous objects cannot define "@"-prefixed keys,
    /// so callers use <c>type = "..."</c> as a convention.
    /// </summary>
    private static void NormalizePropertyNames(JsonObject obj)
    {
        foreach (var name in _propertiesToNormalize)
        {
            if (obj.TryGetPropertyValue(name, out var value))
            {
                obj.Remove(name);

                var newName = "@" + name;
                if (!obj.ContainsKey(newName))
                {
                    obj.Insert(0, newName, value);
                }
            }
        }

        foreach (var (_, value) in obj.ToList())
        {
            if (value is JsonObject nested)
            {
                NormalizePropertyNames(nested);
            }
            else if (value is JsonArray array)
            {
                foreach (var item in array)
                {
                    if (item is JsonObject nestedItem) NormalizePropertyNames(nestedItem);
                }
            }
        }
    }

    private static void SetOrMerge(JsonObject target, string key, JsonNode? value)
    {
        if (!target.ContainsKey(key))
        {
            target[key] = value;
        }
        else if (target[key] is JsonObject existingObj && value is JsonObject newObj)
        {
            DeepMerge(existingObj, newObj);
        }
        // else: key exists and is not an object --> first-write wins, do nothing
    }

    private static void DeepMerge(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (!target.ContainsKey(key))
            {
                target[key] = value?.DeepClone();
            }
            else if (target[key] is JsonObject existingNested && value is JsonObject newNested)
            {
                DeepMerge(existingNested, newNested);
            }
            // else: first-write wins
        }
    }
}
