using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters;

internal class TimeSpanConverter : DefaultTypeConverter
{
    public TimeSpanConverter()
        : base(typeof(TimeSpan))
    {
    }

    public override bool CanConvertFrom(Type type)
        => type == typeof(string) || type == typeof(DateTime) || base.CanConvertFrom(type);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value is null)
            return base.ConvertFrom(culture, value);

        // Fast paths first
        if (value is TimeSpan ts)
            return ts;

        if (value is DateTime dt)
            return new TimeSpan(dt.Ticks);

        if (value is string s)
        {
            if (s.Length == 0)
                return base.ConvertFrom(culture, value);

            // Avoids NumberStyles.None overhead and handles leading/trailing whitespace.
            s = s.Trim();

            if (TimeSpan.TryParse(s, culture, out var span))
                return span;

            if (long.TryParse(s, NumberStyles.Integer, culture, out var lng))
            {
                // Preserve existing behavior: treat as unix time, then convert to ticks -> TimeSpan
                return new TimeSpan(lng.FromUnixTime().Ticks);
            }

            if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var dbl))
            {
                // Preserve existing behavior: treat as OADate, then convert to ticks -> TimeSpan
                return new TimeSpan(DateTime.FromOADate(dbl).Ticks);
            }

            return base.ConvertFrom(culture, value);
        }

        // Avoid exception-driven control flow: Convert.ChangeType(TimeSpan) is rarely supported for most types.
        // Keep a couple of cheap, common numeric conversions explicitly.
        if (value is long l)
            return new TimeSpan(l);

        if (value is int i)
            return new TimeSpan(i);

        if (value is double d)
            return TimeSpan.FromTicks((long)d);

        if (value is IConvertible)
        {
            try
            {
                return (TimeSpan)System.Convert.ChangeType(value, typeof(TimeSpan), culture);
            }
            catch
            {
                // preserve old behavior (fallback to base)
            }
        }

        return base.ConvertFrom(culture, value);
    }
}
