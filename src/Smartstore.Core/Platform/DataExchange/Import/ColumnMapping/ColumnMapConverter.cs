using System.Globalization;
using Newtonsoft.Json;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Core.DataExchange.Import
{
    public class ColumnMapConverter : DefaultTypeConverter
    {
        public ColumnMapConverter()
            : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
            => type == typeof(string);

        public override bool CanConvertTo(Type type)
            => type == typeof(string);

        public T ConvertFrom<T>(string value)
            => value.HasValue() ? (T)ConvertFrom(CultureInfo.InvariantCulture, value) : default;

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string strValue)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, ColumnMappingItem>>(strValue);
                var map = new ColumnMap();

                foreach (var kvp in dict)
                {
                    map.AddMapping(kvp.Key, null, kvp.Value.MappedName, kvp.Value.Default);
                }

                return map;
            }

            return base.ConvertFrom(culture, value);
        }

        public string ConvertTo(object value)
            => (string)ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(string));

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to == typeof(string))
            {
                if (value is ColumnMap map)
                {
                    return JsonConvert.SerializeObject(map.Mappings);
                }
                else
                {
                    return string.Empty;
                }
            }

            return base.ConvertTo(culture, format, value, to);
        }
    }
}
