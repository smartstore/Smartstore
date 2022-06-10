using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class StringValuesConverter : DefaultTypeConverter
    {
        public StringValuesConverter()
            : base(typeof(StringValues))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string)
                || typeof(IEnumerable<string>).IsAssignableFrom(type)
                || base.CanConvertFrom(type);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is string str)
            {
                return new StringValues(str);
            }

            if (value is IEnumerable<string> list)
            {
                return new StringValues(list.ToArray());
            }

            return base.ConvertFrom(culture, value);
        }
    }
}
