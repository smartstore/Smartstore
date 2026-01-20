using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Smartstore.Json;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class JsonObjectConverter : DefaultTypeConverter
    {
        public JsonObjectConverter()
            : base(typeof(JsonObject))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string)
                || typeof(IDictionary<string, object>).IsAssignableFrom(type)
                || type.IsPlainObjectType()
                || type.IsAnonymousType();
        }

        public override bool CanConvertTo(Type type)
        {
            return type == typeof(string)
                || typeof(IDictionary<string, object>).IsAssignableFrom(type);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string json)
            {
                return JsonNode.Parse(json)?.AsObject();
            }

            if (value != null)
            {
                return JsonSerializer.SerializeToNode(value, SmartJsonOptions.Default)?.AsObject();
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (value is JsonObject jobj)
            {
                if (to == typeof(string))
                {
                    return jobj.ToJsonString(SmartJsonOptions.Default);
                }
                else
                {
                    return ConvertUtility.ObjectToDictionary(jobj, null);
                }
            }

            return base.ConvertTo(culture, format, value, to);
        }
    }
}
