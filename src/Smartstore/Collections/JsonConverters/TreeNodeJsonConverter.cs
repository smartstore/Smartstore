using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Smartstore.Collections.JsonConverters
{
    internal sealed class TreeNodeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            var canConvert = objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(TreeNode<>);
            return canConvert;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var valueType = objectType.GetGenericArguments()[0];
            var sequenceType = typeof(List<>).MakeGenericType(objectType);

            object objValue = null;
            object objChildren = null;
            object id = null;
            Dictionary<string, object> metadata = null;

            reader.Read();
            while (reader.TokenType == JsonToken.PropertyName)
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

                    if (id is JArray jarr)
                    {
                        id = jarr.Select(token =>
                        {
                            // Newtonsoft holds ints as Int64, but we need Int32 here.
                            return token.Type == JTokenType.Integer
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
                ? new object[] { objValue, objChildren }
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

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
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

                    var typeNameHandling = serializer.TypeNameHandling;
                    try
                    {
                        serializer.TypeNameHandling = TypeNameHandling.All;
                        serializer.Serialize(writer, dict);
                    }
                    finally
                    {
                        serializer.TypeNameHandling = typeNameHandling;
                    }
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
        {
            return instance.GetType().GetProperty(name).GetValue(instance);
        }
    }
}