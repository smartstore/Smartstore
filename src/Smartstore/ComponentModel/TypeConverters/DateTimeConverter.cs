using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class DateTimeConverter : DefaultTypeConverter
    {
        public DateTimeConverter()
            : base(typeof(DateTime))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            return type == typeof(string)
                || type == typeof(long)
                || type == typeof(double)
                || type == typeof(TimeSpan)
                || base.CanConvertFrom(type);
        }

        public override bool CanConvertTo(Type type)
        {
            return type == typeof(string)
                || type == typeof(long)
                || type == typeof(double)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || base.CanConvertTo(type);
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is TimeSpan span)
            {
                return new DateTime(span.Ticks);
            }

            if (value is string str)
            {
                if (DateTime.TryParse(str, culture, DateTimeStyles.None, out var time))
                {
                    return time;
                }

                if (long.TryParse(str, NumberStyles.None, culture, out var lng))
                {
                    return lng.FromUnixTime();
                }

                if (double.TryParse(str, NumberStyles.AllowDecimalPoint, culture, out var dbl))
                {
                    return DateTime.FromOADate(dbl);
                }
            }

            if (value is long lng2)
            {
                return lng2.FromUnixTime();
            }

            if (value is double dbl2)
            {
                return DateTime.FromOADate(dbl2);
            }

            return base.ConvertFrom(culture, value);
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            var time = (DateTime)value;

            if (to == typeof(DateTimeOffset))
            {
                return new DateTimeOffset(time);
            }
            if (to == typeof(TimeSpan))
            {
                return new TimeSpan(time.Ticks);
            }
            if (to == typeof(double))
            {
                return time.ToOADate();
            }
            if (to == typeof(long))
            {
                return time.ToUnixTime();
            }

            return base.ConvertTo(culture, format, value, to);
        }
    }
}
