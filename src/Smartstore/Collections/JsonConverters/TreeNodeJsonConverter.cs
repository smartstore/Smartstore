using System.Text.Json;
using System.Text.Json.Serialization;
using Smartstore.Json;
using Smartstore.Json.Polymorphy;

namespace Smartstore.Collections.JsonConverters;

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