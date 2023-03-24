using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class JObjectConverter : DefaultTypeConverter
    {
        public JObjectConverter()
            : base(typeof(JObject))
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
                return JObject.Parse(json);
            }

            if (value != null)
            {
                return JObject.FromObject(value);
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (value is JObject jobj)
            {
                if (to == typeof(string))
                {
                    return jobj.ToString(Formatting.Indented);
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
