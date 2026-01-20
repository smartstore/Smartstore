#nullable enable

using System.Dynamic;
using System.Text.Json.Nodes;
using System.Collections;
using System.Text.Json;

namespace Smartstore.Json;

public static class JsonNodeExtensions
{
    /// <summary>
    /// Extension to easily convert any JsonNode into a dynamic wrapper.
    /// </summary>
    public static dynamic ToDynamic(this JsonNode node)
    {
        return new DynamicJsonNode(node);
    }
}

/// <summary>
/// A wrapper around JsonNode that enables dynamic dot-notation access and IDictionary behavior.
/// Mimics the behavior of Newtonsoft.Json JObject / ExpandoObject.
/// </summary>
public class DynamicJsonNode(JsonNode? node) : DynamicObject, IDictionary<string, object?>
{
    // The underlying STJ node
    private readonly JsonNode? _node = node;

    // --- DynamicObject Implementation (The core for dot-notation) ---

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = null;

        // We only allow member access on JsonObjects
        if (_node is not JsonObject jsonObj)
            return false;

        // Try to retrieve the property (case-sensitive by default in STJ)
        if (jsonObj.TryGetPropertyValue(binder.Name, out var valueNode))
        {
            // Recursively convert the result so chained access (payload.a.b) works
            result = ConvertNode(valueNode);
            return true;
        }

        // If property does not exist, return null (similar to JObject dynamic behavior)
        // rather than throwing a RuntimeBinderException.
        result = null;
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        if (_node is not JsonObject jsonObj)
            return false;

        // Convert the value back to a JsonNode and set it
        jsonObj[binder.Name] = ConvertValue(value);
        return true;
    }

    // Optional: Enables indexer access via dynamic (payload["Member"])
    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
    {
        if (indexes.Length == 1 && indexes[0] is string key && _node is JsonObject jsonObj)
        {
            var success = jsonObj.TryGetPropertyValue(key, out var val);
            result = success ? ConvertNode(val) : null;
            return true;
        }

        // Support for array index access (payload[0])
        if (indexes.Length == 1 && indexes[0] is int index && _node is JsonArray jsonArray)
        {
            if (index >= 0 && index < jsonArray.Count)
            {
                result = ConvertNode(jsonArray[index]);
                return true;
            }
        }

        result = null;
        return false;
    }

    // --- Conversion Helpers (The "Glue" logic) ---

    /// <summary>
    /// Converts a JsonNode into a dynamic wrapper (for objects) or native value (for primitives).
    /// </summary>
    private static object? ConvertNode(JsonNode? node)
    {
        return node switch
        {
            null => null,
            // If it is an object, wrap it again to support payload.Sub.Sub
            JsonObject obj => new DynamicJsonNode(obj),
            // If it is an array, convert items to a list to allow iteration and indexing
            JsonArray arr => arr.Select(ConvertNode).ToList(),
            // If it is a value (String, Number, Bool), retrieve the native C# value
            JsonValue val => ConvertJsonValue(val),
            _ => node
        };
    }

    private static object? ConvertJsonValue(JsonValue val)
    {
        // Nodes created from JsonNode.Parse(...) typically store a JsonElement internally.
        if (val.TryGetValue<JsonElement>(out var el))
        {
            if (el.TryGetScalarValue(out var value))
            {
                return value;
            }

            return el.ValueKind switch
            {
                // These should not usually appear as JsonValue, but handle defensively.
                JsonValueKind.Object => new DynamicJsonNode(JsonNode.Parse(el.GetRawText())!),
                JsonValueKind.Array => JsonNode.Parse(el.GetRawText())!.AsArray().Select(ConvertNode).ToList(),

                _ => el.GetRawText()
            };
        }

        // If it's CLR-backed already (e.g., JsonValue.Create(123m)), this returns the CLR value.
        return val.GetValue<object?>();
    }

    /// <summary>
    /// Converts a raw C# object back into a JsonNode for storage.
    /// </summary>
    private static JsonNode? ConvertValue(object? value)
    {
        if (value is null) return null;
        if (value is JsonNode node) return node; // Already a node
        if (value is DynamicJsonNode wrapper) return ConvertValue(wrapper._node); // Unwrap our own wrapper

        // For primitives, STJ handles creation efficiently
        try
        {
            return JsonValue.Create(value);
        }
        catch
        {
            // Fallback for complex POCOs: serialize them to a Node
            return JsonSerializer.SerializeToNode(value);
        }
    }

    // --- IDictionary<string, object?> Implementation ---
    // Allows passing this object to serializers or mapping tools that expect a dictionary.

    private JsonObject GetObjectOrThrow() => _node as JsonObject
        ?? throw new InvalidOperationException("Underlying Node is not a JSON Object");

    public ICollection<string> Keys => GetObjectOrThrow().Select(x => x.Key).ToList();

    // Values must be converted to ensure consistency
    public ICollection<object?> Values => GetObjectOrThrow().Select(x => ConvertNode(x.Value)).ToList();

    public int Count => GetObjectOrThrow().Count;

    public bool IsReadOnly => false;

    public object? this[string key]
    {
        get
        {
            var obj = GetObjectOrThrow();
            return obj.TryGetPropertyValue(key, out var val) ? ConvertNode(val) : null;
        }
        set
        {
            var obj = GetObjectOrThrow();
            obj[key] = ConvertValue(value);
        }
    }

    public void Add(string key, object? value) => GetObjectOrThrow().Add(key, ConvertValue(value));

    public bool ContainsKey(string key) => GetObjectOrThrow().ContainsKey(key);

    public bool Remove(string key) => GetObjectOrThrow().Remove(key);

    public bool TryGetValue(string key, out object? value)
    {
        var obj = GetObjectOrThrow();
        if (obj.TryGetPropertyValue(key, out var node))
        {
            value = ConvertNode(node);
            return true;
        }
        value = null;
        return false;
    }

    public void Add(KeyValuePair<string, object?> item) => Add(item.Key, item.Value);

    public void Clear() => GetObjectOrThrow().Clear();

    public bool Contains(KeyValuePair<string, object?> item)
    {
        if (TryGetValue(item.Key, out var val))
        {
            return Equals(val, item.Value);
        }
        return false;
    }

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        var collection = GetObjectOrThrow().ToDictionary(k => k.Key, v => ConvertNode(v.Value));
        ((ICollection<KeyValuePair<string, object?>>)collection).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<string, object?> item)
    {
        if (Contains(item)) return Remove(item.Key);
        return false;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        return GetObjectOrThrow()
            .Select(x => new KeyValuePair<string, object?>(x.Key, ConvertNode(x.Value)))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}