using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters;

internal class DateTimeConverter : DefaultTypeConverter
{
    public DateTimeConverter()
        : base(typeof(DateTime))
    {
    }

    public override bool CanConvertFrom(Type type)
        => type == typeof(string)
            || type == typeof(long)
            || type == typeof(double)
            || type == typeof(TimeSpan)
            || base.CanConvertFrom(type);

    public override bool CanConvertTo(Type type)
        => type == typeof(string)
            || type == typeof(long)
            || type == typeof(double)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || base.CanConvertTo(type);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        switch (value)
        {
            case TimeSpan span:
                return new DateTime(span.Ticks);

            case string str:
                // Fast-path: numeric parsing first avoids DateTime.TryParse's comparatively heavy work
                // for common cases like unix timestamps or OA dates.
                if (long.TryParse(str, NumberStyles.None, culture, out var unix))
                {
                    return unix.FromUnixTime();
                }

                if (double.TryParse(str, NumberStyles.AllowDecimalPoint, culture, out var oa))
                {
                    return DateTime.FromOADate(oa);
                }

                if (DateTime.TryParse(str, culture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }

                break;

            case long unix2:
                return unix2.FromUnixTime();

            case double oa2:
                return DateTime.FromOADate(oa2);
        }

        return base.ConvertFrom(culture, value);
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        var time = (DateTime)value;

        if (to == typeof(DateTimeOffset))
            return new DateTimeOffset(time);

        if (to == typeof(TimeSpan))
            return new TimeSpan(time.Ticks);

        if (to == typeof(double))
            return time.ToOADate();

        if (to == typeof(long))
            return time.ToUnixTime();

        return base.ConvertTo(culture, format, value, to);
    }
}
