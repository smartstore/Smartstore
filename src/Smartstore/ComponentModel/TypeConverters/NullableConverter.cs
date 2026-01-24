using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters;

internal class NullableConverter : DefaultTypeConverter
{
    private readonly bool _elementTypeIsConvertible;
    private readonly Type _elementType;

    internal NullableConverter(Type type, Type elementType)
        : base(type)
    {
        NullableType = type;
        _elementType = elementType ?? Nullable.GetUnderlyingType(type)
            ?? throw Error.Argument("type", "Type is not a nullable type.");

        ElementType = _elementType;

        _elementTypeIsConvertible = typeof(IConvertible).IsAssignableFrom(_elementType) && !_elementType.IsEnum;
        ElementTypeConverter = TypeConverterFactory.GetConverter(_elementType);
    }

    public Type NullableType { get; }

    public Type ElementType { get; }

    public ITypeConverter ElementTypeConverter { get; }

    public override bool CanConvertFrom(Type type)
    {
        // order in likelihood: exact match, converter says yes, numeric convertible fast-path
        if (type == _elementType)
            return true;

        if (ElementTypeConverter.CanConvertFrom(type))
            return true;

        return _elementTypeIsConvertible
            && type != typeof(string)
            && typeof(IConvertible).IsAssignableFrom(type);
    }

    public override bool CanConvertTo(Type type)
        => type == _elementType || ElementTypeConverter.CanConvertTo(type);

    public override object ConvertFrom(CultureInfo culture, object value)
    {
        if (value is null)
            return null;

        var valueType = value.GetType();
        if (valueType == _elementType)
            return value;

        // Common: empty string => null
        if (value is string s)
            return s.Length == 0 ? null : ElementTypeConverter.ConvertFrom(culture, s);

        // num -> num?
        if (_elementTypeIsConvertible && value is IConvertible)
            return Convert.ChangeType(value, _elementType, culture);

        return ElementTypeConverter.ConvertFrom(culture, value);
    }

    public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        if (to == _elementType && value is not null && NullableType.IsInstanceOfType(value))
            return value;

        if (value is null && to == typeof(string))
            return string.Empty;

        return ElementTypeConverter.ConvertTo(culture, format, value, to);
    }
}
