using System.Collections.Frozen;
using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class BooleanConverter : DefaultTypeConverter
    {
        private readonly FrozenSet<string> _trueValues;
        private readonly FrozenSet<string> _falseValues;

        public BooleanConverter(string[] trueValues, string[] falseValues)
            : base(typeof(bool))
        {
            _trueValues = trueValues.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            _falseValues = falseValues.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }

        public ICollection<string> TrueValues
        {
            get { return _trueValues; }
        }

        public ICollection<string> FalseValues
        {
            get { return _falseValues; }
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            if (value is short shrt)
            {
                if (shrt == 0)
                {
                    return false;
                }
                if (shrt == 1)
                {
                    return true;
                }
            }

            if (value is string str)
            {
                if (bool.TryParse(str, out var b))
                {
                    return b;
                }

                if (short.TryParse(str, out var sh))
                {
                    if (sh == 0)
                    {
                        return false;
                    }
                    if (sh == 1)
                    {
                        return true;
                    }
                }

                str = (str.NullEmpty() ?? string.Empty).Trim();
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
}
