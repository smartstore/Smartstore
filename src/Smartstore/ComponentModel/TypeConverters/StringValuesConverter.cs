using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Smartstore.ComponentModel.TypeConverters;

internal class StringValuesConverter : DefaultTypeConverter
{
    public StringValuesConverter()
        : base(typeof(StringValues))
    {
    }

    public override bool CanConvertFrom(Type type)
    {
        // Fast-path the common cases without reflection-heavy checks.
        if (type == typeof(string) || type == typeof(string[]))
            return true;

        // Keep compatibility with other string enumeration types.
        if (typeof(IEnumerable<string>).IsAssignableFrom(type))
            return true;

        return base.CanConvertFrom(type);
    }

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value is string str)
            return new StringValues(str);

        // Avoid ToArray() allocation when we already have an array.
        if (value is string[] arr)
            return new StringValues(arr);

        // If it's a concrete collection, size is known; copy directly (usually faster than LINQ's ToArray()).
        if (value is ICollection<string> coll)
        {
            if (coll.Count == 0)
                return default(StringValues);

            var buffer = new string[coll.Count];
            coll.CopyTo(buffer, 0);
            return new StringValues(buffer);
        }

        if (value is IEnumerable<string> seq)
        {
            // Fallback for non-collection enumerables.
            return new StringValues(seq.ToArray());
        }

        return base.ConvertFrom(culture, value);
    }
}
