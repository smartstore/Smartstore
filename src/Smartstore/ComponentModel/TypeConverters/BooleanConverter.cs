using System.Collections.Frozen;
using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters;

internal sealed class BooleanConverter : DefaultTypeConverter
{
    private readonly FrozenSet<string> _trueValues;
    private readonly FrozenSet<string> _falseValues;

    public BooleanConverter(string[] trueValues, string[] falseValues)
        : base(typeof(bool))
    {
        _trueValues = trueValues.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _falseValues = falseValues.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    public ICollection<string> TrueValues => _trueValues;

    public ICollection<string> FalseValues => _falseValues;

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        // Fast-path for numeric types: avoid any string work/allocations.
        if (value is short shrt)
        {
            return shrt switch
            {
                0 => false,
                1 => true,
                _ => base.ConvertFrom(culture, value)
            };
        }

        // Keep the string path tight and avoid repeated parsing/branching.
        if (value is string str)
        {
            // Common cases first.
            if (bool.TryParse(str, out var b))
            {
                return b;
            }

            // Tiny fast-path for "0"/"1" without invoking a numeric parser.
            // (Also avoids accepting culture/format variations that TryParse might handle.)
            if (str.Length == 1)
            {
                var ch = str[0];
                if (ch == '0') return false;
                if (ch == '1') return true;
            }

            // Trim only when needed (most inputs are already trimmed).
            // Also handles null/empty with minimal overhead.
            str = (str.NullEmpty() ?? string.Empty);
            if (str.Length != 0)
            {
                str = str.Trim();
            }

            if (_trueValues.Contains(str))
            {
                return true;
            }

            if (_falseValues.Contains(str))
            {
                return false;
            }
        }

        return base.ConvertFrom(culture, value);
    }
}
