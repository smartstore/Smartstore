using Microsoft.Extensions.Primitives;

namespace Smartstore.ComponentModel.TypeConverters
{
    public class SimpleTypeConverterProvider : ITypeConverterProvider
    {
        public ITypeConverter GetConverter(Type type)
        {
            if (type == typeof(DateTime))
            {
                return new DateTimeConverter();
            }
            else if (type == typeof(TimeSpan))
            {
                return new TimeSpanConverter();
            }
            else if (type == typeof(StringValues))
            {
                return new StringValuesConverter();
            }
            else if (type == typeof(bool))
            {
                return new BooleanConverter(
                    new[] { "1", "yes", "y", "on", "wahr" },
                    new[] { "0", "no", "n", "off", "falsch" });
            }
            else if (type.IsNullableType(out Type elementType))
            {
                return new NullableConverter(type, elementType);
            }

            return null;
        }
    }
}
