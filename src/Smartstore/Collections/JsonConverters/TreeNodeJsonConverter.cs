using System.Text.Json;
using System.Text.Json.Serialization;
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
    public override TreeNode<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        T value = default;
        List<TreeNode<T>> children = null;
        object id = null;
        Dictionary<string, object> metadata = null;

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
                value = JsonSerializer.Deserialize<T>(ref reader, options);
            }
            else if (string.Equals(propertyName, "Metadata", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: (json) (mc) Polymorphy!
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            }
            else if (string.Equals(propertyName, "Children", StringComparison.OrdinalIgnoreCase))
            {
                children = JsonSerializer.Deserialize<List<TreeNode<T>>>(ref reader, options);
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
                        if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int intValue))
                        {
                            idList.Add(intValue);
                        }
                        else
                        {
                            idList.Add(item.Deserialize<object>(options));
                        }
                    }
                    id = idList.ToArray();
                }
                else
                {
                    id = element.Deserialize<object>(options);
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
        JsonSerializer.Serialize(writer, value.Value, options);

        // Metadata
        if (value.Metadata != null && value.Metadata.Count > 0)
        {
            writer.WritePropertyName("Metadata");
            // TODO: (json) (mc) Polymorphy!
            JsonSerializer.Serialize(writer, value.Metadata, options);
        }

        // Children
        if (value.HasChildren)
        {
            writer.WritePropertyName("Children");
            JsonSerializer.Serialize(writer, value.Children, options);
        }

        writer.WriteEndObject();
    }
}