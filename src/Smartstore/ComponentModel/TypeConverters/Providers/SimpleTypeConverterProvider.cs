using Microsoft.Extensions.Primitives;

namespace Smartstore.ComponentModel.TypeConverters;

public class SimpleTypeConverterProvider : ITypeConverterProvider
{
    // These converters are stateless; reuse instances to avoid allocations on every lookup.
    private static readonly ITypeConverter DateTime = new DateTimeConverter();
    private static readonly ITypeConverter TimeSpan = new TimeSpanConverter();
    private static readonly ITypeConverter StringValues = new StringValuesConverter();

    // BooleanConverter is also effectively stateless given fixed token sets.
    private static readonly ITypeConverter Boolean = new BooleanConverter(
        ["1", "yes", "y", "on", "wahr", "true,false"],
        ["0", "no", "n", "off", "falsch"]);

    public ITypeConverter GetConverter(Type type)
    {
        if (type == typeof(DateTime))
            return DateTime;

        if (type == typeof(TimeSpan))
            return TimeSpan;

        if (type == typeof(StringValues))
            return StringValues;

        if (type == typeof(bool))
            return Boolean;

        // Nullable converter depends on (type, elementType), so don't cache the instance here.
        if (type.IsNullableType(out var elementType))
            return new NullableConverter(type, elementType);

        return null;
    }
}
