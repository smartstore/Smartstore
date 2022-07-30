using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class TimeSpanConverter : DefaultTypeConverter
    {
        public TimeSpanConverter()
            : base(typeof(TimeSpan))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string)
                || type == typeof(DateTime)
                || base.CanConvertFrom(type);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is DateTime time)
            {
                return new TimeSpan(time.Ticks);
            }

            if (value is string str)
            {
                if (TimeSpan.TryParse(str, culture, out var span))
                {
                    return span;
                }

                if (long.TryParse(str, NumberStyles.None, culture, out var lng))
                {
                    return new TimeSpan(lng.FromUnixTime().Ticks);
                }

                if (double.TryParse(str, NumberStyles.None, culture, out var dbl))
                {
                    return new TimeSpan(DateTime.FromOADate(dbl).Ticks);
                }
            }

            try
            {
                return (TimeSpan)System.Convert.ChangeType(value, typeof(TimeSpan), culture);
            }
            catch
            {
            }

            return base.ConvertFrom(culture, value);
        }
    }
}
