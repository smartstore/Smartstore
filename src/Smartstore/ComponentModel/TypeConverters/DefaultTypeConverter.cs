using System.ComponentModel;
using System.Globalization;

namespace Smartstore.ComponentModel.TypeConverters;

public class DefaultTypeConverter : ITypeConverter
{
    private static readonly Type ObjectType = typeof(object);
    private static readonly Type StringType = typeof(string);
    private static readonly Type ConvertibleType = typeof(IConvertible);

    private readonly Lazy<TypeConverter> _systemConverter;
    private readonly Type _type;
    private readonly bool _typeIsConvertible;
    private readonly bool _typeIsEnum;

    public DefaultTypeConverter(Type type)
    {
        Guard.NotNull(type);

        _type = type;
        _typeIsConvertible = ConvertibleType.IsAssignableFrom(type);
        _typeIsEnum = type.IsEnum;

        // Avoid eager TypeDescriptor work; make thread-safe as before.
        _systemConverter = new Lazy<TypeConverter>(() => TypeDescriptor.GetConverter(type), isThreadSafe: true);
    }

    public TypeConverter SystemConverter
        => _type == ObjectType ? null : _systemConverter.Value;

    public virtual bool CanConvertFrom(Type type)
    {
        // Fast path: if source is IConvertible and target is convertible/enum, ConvertFrom can use ChangeType/Enum.ToObject.
        if (ConvertibleType.IsAssignableFrom(type) && (_typeIsConvertible || _typeIsEnum))
        {
            return true;
        }

        var conv = SystemConverter;
        return conv != null && conv.CanConvertFrom(type);
    }

    public virtual bool CanConvertTo(Type type)
    {
        // Fast path: use Convert.ChangeType if both are IConvertible.
        if (_typeIsConvertible && ConvertibleType.IsAssignableFrom(type))
        {
            return true;
        }

        // String is always supported by ToString() fallback.
        if (type == StringType)
        {
            return true;
        }

        var conv = SystemConverter;
        return conv != null && conv.CanConvertTo(type);
    }

    public virtual object ConvertFrom(CultureInfo culture, object value)
    {
        if (value is null)
        {
            // Keep existing behavior: system converter might handle null, otherwise this would previously throw on value.GetType().
            var convNull = SystemConverter;
            if (convNull != null)
            {
                return convNull.ConvertFrom(null, culture, null);
            }

            // Match previous failure mode as closely as possible.
            throw Error.InvalidCast(ObjectType, _type);
        }

        if (ReferenceEquals(value.GetType(), _type))
        {
            return value;
        }

        // Use Convert.ChangeType if both types are IConvertible (excluding string).
        if (!_typeIsEnum && _typeIsConvertible && value is IConvertible && value is not string)
        {
            return Convert.ChangeType(value, _type, culture);
        }

        // Use Enum.ToObject if type is Enum and value is numeric.
        var valueType = value.GetType();
        if (_typeIsEnum && valueType.IsPrimitive)
        {
            return Enum.ToObject(_type, value);
        }

        var conv = SystemConverter;
        if (conv != null)
        {
            return conv.ConvertFrom(null, culture, value);
        }

        throw Error.InvalidCast(valueType, _type);
    }

    public virtual object ConvertTo(CultureInfo culture, string format, object value, Type to)
    {
        // Use Convert.ChangeType if both types are IConvertible.
        if (!_typeIsEnum
            && _typeIsConvertible
            && value is not null
            && value is not string
            && ConvertibleType.IsAssignableFrom(to))
        {
            return Convert.ChangeType(value, to, culture);
        }

        var conv = SystemConverter;
        if (conv != null)
        {
            return conv.ConvertTo(null, culture, value, to);
        }

        // Fallback to string representation (keeps existing behavior).
        return value is null ? string.Empty : value.ToString();
    }
}
