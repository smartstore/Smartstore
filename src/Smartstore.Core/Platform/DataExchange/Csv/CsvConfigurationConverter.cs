using System.Globalization;
using Newtonsoft.Json;
using Smartstore.ComponentModel.TypeConverters;

namespace Smartstore.Core.DataExchange.Csv
{
    public class CsvConfigurationConverter : DefaultTypeConverter
    {
        public CsvConfigurationConverter()
            : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
            => type == typeof(string);

        public override bool CanConvertTo(Type type)
            => type == typeof(string);

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string strValue)
            {
                return JsonConvert.DeserializeObject<CsvConfiguration>(strValue);
            }

            return base.ConvertFrom(culture, value);
        }

        public T ConvertFrom<T>(string value)
        {
            if (value.HasValue())
                return (T)ConvertFrom(CultureInfo.InvariantCulture, value);

            return default;
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            if (to == typeof(string))
            {
                if (value is CsvConfiguration)
                {
                    return JsonConvert.SerializeObject(value);
                }
                else
                {
                    return string.Empty;
                }
            }

            return base.ConvertTo(culture, format, value, to);
        }

        public string ConvertTo(object value)
        {
            return (string)ConvertTo(CultureInfo.InvariantCulture, null, value, typeof(string));
        }
    }
}
