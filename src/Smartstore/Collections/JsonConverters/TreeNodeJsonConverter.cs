using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Json;
using Smartstore.Json.Polymorphy;
using NSJ = Newtonsoft.Json;
using NSJL = Newtonsoft.Json.Linq;

namespace Smartstore.Collections.JsonConverters;

internal sealed class TreeNodeJsonConverter : NSJ.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(TreeNode<>);
        return canConvert;
    }

    public override object ReadJson(NSJ.JsonReader reader, Type objectType, object existingValue, NSJ.JsonSerializer serializer)
    {
        var valueType = objectType.GetGenericArguments()[0];
        var sequenceType = typeof(List<>).MakeGenericType(objectType);

        object objValue = null;
        object objChildren = null;
        object id = null;
        Dictionary<string, object> metadata = null;

        reader.Read();
        while (reader.TokenType == NSJ.JsonToken.PropertyName)
        {
            string a = reader.Value.ToString();
            if (string.Equals(a, "Value", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                objValue = serializer.Deserialize(reader, valueType);
            }
            else if (string.Equals(a, "Metadata", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                metadata = serializer.Deserialize<Dictionary<string, object>>(reader);
            }
            else if (string.Equals(a, "Children", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                objChildren = serializer.Deserialize(reader, sequenceType);
            }
            else if (string.Equals(a, "Id", StringComparison.OrdinalIgnoreCase))
            {
                reader.Read();
                id = serializer.Deserialize<object>(reader);

                if (id is NSJL.JArray jarr)
                {
                    id = jarr.Select(token =>
                    {
                        // Newtonsoft holds ints as Int64, but we need Int32 here.
                        return token.Type == NSJL.JTokenType.Integer
                            ? token.ToObject<int>()
                            : token.ToObject<object>();
                    })
                    .ToArray();
                }
            }
            else
            {
                reader.Skip();
            }

            reader.Read();
        }

        var ctorParams = objChildren != null
            ? [objValue, objChildren]
            : new object[] { objValue };

        var treeNode = Activator.CreateInstance(objectType, ctorParams);

        // Set Metadata
        if (metadata != null && metadata.Count > 0)
        {
            var metadataProp = objectType.GetProperty("Metadata");
            metadataProp.SetValue(treeNode, metadata);
        }

        // Set Id
        if (id != null)
        {
            var idProp = objectType.GetProperty("Id");
            idProp.SetValue(treeNode, id);
        }

        return treeNode;
    }

    public override void WriteJson(NSJ.JsonWriter writer, object value, NSJ.JsonSerializer serializer)
    {
        writer.WriteStartObject();
        {
            // Id
            if (GetPropValue("Id", value) is object o)
            {
                writer.WritePropertyName("Id");
                serializer.Serialize(writer, o);
            }

            // Value
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, GetPropValue("Value", value));

            // Metadata
            if (GetPropValue("Metadata", value) is IDictionary<string, object> dict && dict.Count > 0)
            {
                writer.WritePropertyName("Metadata");
                serializer.SerializeObjectDictionary(writer, dict);
            }

            // Children
            if (GetPropValue("HasChildren", value) is bool b && b == true)
            {
                writer.WritePropertyName("Children");
                serializer.Serialize(writer, GetPropValue("Children", value));
            }
        }
        writer.WriteEndObject();
    }

    private static object GetPropValue(string name, object instance)
        => instance.GetType().GetProperty(name).GetValue(instance);
}

internal sealed class TreeNodeJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(TreeNode<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type wrappedType = typeToConvert.GetGenericArguments()[0];
        Type converterType = typeof(TreeNodeJsonConverter<>).MakeGenericType(wrappedType);

        return (JsonConverter)Activator.CreateInstance(converterType);
    }
}

internal sealed class TreeNodeJsonConverter<T> : JsonConverter<TreeNode<T>>
{
    private readonly bool _isPolymorphicValueType;

    public TreeNodeJsonConverter()
    {
        _isPolymorphicValueType = PolymorphyCodec.TryGetPolymorphyKind(typeof(T), out _, out _);
        if (_isPolymorphicValueType)
        {
            // Polymorhic types with a custom converter (e.g. IPermissionNode) can be handled by STJ directly.
            _isPolymorphicValueType = !typeof(T).HasAttribute<JsonConverterAttribute>(false);
        }
    }
    
    public override TreeNode<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        T value = default;
        List<TreeNode<T>> children = null;
        object id = null;
        IDictionary<string, object> metadata = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString();
            reader.Read();

            if (string.Equals(propertyName, "Value", StringComparison.OrdinalIgnoreCase))
            {
                if (_isPolymorphicValueType)
                {
                    value = options.DeserializePolymorphic<T>(ref reader);
                }
                else
                {
                    var declaredType = DefaultImplementationAttribute.Resolve(typeof(T));
                    value = (T)JsonSerializer.Deserialize(ref reader, declaredType, options);
                }
            }
            else if (string.Equals(propertyName, "Metadata", StringComparison.OrdinalIgnoreCase))
            {
                metadata = options.DeserializePolymorphic<IDictionary<string, object>>(ref reader);
            }
            else if (string.Equals(propertyName, "Children", StringComparison.OrdinalIgnoreCase))
            {
                if (_isPolymorphicValueType)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        throw new JsonException();
                    }

                    children = [];

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }

                        // Recursion: will use TreeNodeJsonConverter<T> again via the [JsonConverter] attribute/factory.
                        var child = JsonSerializer.Deserialize<TreeNode<T>>(ref reader, options);
                        if (child != null)
                        {
                            children.Add(child);
                        }
                    }
                }
                else
                {
                    children = JsonSerializer.Deserialize<List<TreeNode<T>>>(ref reader, options);
                }
            }
            else if (string.Equals(propertyName, "Id", StringComparison.OrdinalIgnoreCase))
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var element = doc.RootElement;

                if (element.ValueKind == JsonValueKind.Array)
                {
                    var idList = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        idList.Add(CoerceJsonElementToClrValue(item));
                    }
                    id = idList.ToArray();
                }
                else
                {
                    id = CoerceJsonElementToClrValue(element);
                }
            }
        }

        var treeNode = children != null
            ? new TreeNode<T>(value, children)
            : new TreeNode<T>(value);

        if (metadata != null && metadata.Count > 0)
        {
            treeNode.Metadata = metadata;
        }

        if (id != null)
        {
            treeNode.Id = id;
        }

        return treeNode;
    }

    public override void Write(Utf8JsonWriter writer, TreeNode<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Id
        if (value.Id != null)
        {
            writer.WritePropertyName("Id");
            JsonSerializer.Serialize(writer, value.Id, options);
        }

        // Value
        writer.WritePropertyName("Value");
        if (_isPolymorphicValueType)
        {
            options.SerializePolymorphic(writer, value.Value);
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        // Metadata
        if (value.Metadata != null && value.Metadata.Count > 0)
        {
            writer.WritePropertyName("Metadata");
            options.SerializePolymorphic(writer, value.Metadata, wrapArrays: true);
        }

        // Children
        if (value.HasChildren)
        {
            writer.WritePropertyName("Children");
            JsonSerializer.Serialize(writer, value.Children, options);
        }

        writer.WriteEndObject();
    }

    private static object CoerceJsonElementToClrValue(JsonElement element)
    {
        if (element.TryGetScalarValue(out var scalarValue))
        {
            return scalarValue;
        }

        // Avoid leaking JsonElement into Id for complex types:
        // represent objects/arrays as raw JSON string (stable + hashable as dictionary key).
        return element.GetRawText();
    }
}