using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Smartstore.ComponentModel.JsonConverters
{
    public abstract class ObjectContainerJsonConverter<T> : JsonConverter<T>
        where T : IObjectContainer, new()
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IObjectContainer).IsAssignableFrom(typeof(T));
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new T();

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            // Read ValueType
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString();
            if (propertyName != nameof(IObjectContainer.ValueType))
            {
                throw new JsonException();
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var valueTypeStr = reader.GetString();
            Type valueType = Type.GetType(valueTypeStr);
            result.ValueType = valueType;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return result;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Value":
                            result.Value = JsonSerializer.Deserialize(ref reader, valueType, options);
                            break;
                        default:
                            // Let inheritor read and assign property
                            ReadProperty(ref reader, propertyName, result, options);
                            break;
                    }
                }
            }

            throw new JsonException();
        }

        protected virtual void ReadProperty(ref Utf8JsonReader reader, string propertyName, T result, JsonSerializerOptions options)
        {
            //
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartObject();

            // Write ValueType
            var valueType = value.ValueType ?? value.Value?.GetType() ?? typeof(object);
            writer.WriteString(nameof(value.ValueType), valueType.AssemblyQualifiedNameWithoutVersion());

            // Write Value
            writer.WritePropertyName(nameof(value.Value));
            if (value.Value != null)
            {
                JsonSerializer.Serialize(writer, value.Value, valueType, options);
            }
            else
            {
                writer.WriteNullValue();
            }

            // Let inheritor write remaining members
            WriteCore(writer, value, options);

            writer.WriteEndObject();
        }

        protected virtual void WriteCore(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            //
        }
    }

}
