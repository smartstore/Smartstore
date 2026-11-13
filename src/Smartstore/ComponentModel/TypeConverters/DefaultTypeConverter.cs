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
    private readonly bool _hasExplicitConverter;

    public DefaultTypeConverter(Type type)
        : this(type, null)
    {
    }

    /// <summary>
    /// Creates a converter for <paramref name="type"/> with an optional explicit System converter factory.
    /// </summary>
    /// <param name="type">The type being converted.</param>
    /// <param name="converterFactory">A factory evaluated lazily once. When supplied, its non-null converter
    /// handles all capability checks and conversions without default fast paths or fallbacks.
    /// Pass null to use the standard type-based conversion behavior.</param>
    public DefaultTypeConverter(Type type, Func<TypeConverter> converterFactory)
    {
        Guard.NotNull(type);

        _type = type;
        _typeIsConvertible = ConvertibleType.IsAssignableFrom(type);
        _typeIsEnum = type.IsEnum;

        // An explicit converter owns conversion semantics, including unsupported conversions.
        // Keep both explicit construction and default TypeDescriptor lookup lazy and thread-safe.
        _hasExplicitConverter = converterFactory != null;
        _systemConverter = new Lazy<TypeConverter>(converterFactory ?? (() => TypeDescriptor.GetConverter(type)), isThreadSafe: true);
    }

    public TypeConverter SystemConverter
        => !_hasExplicitConverter && _type == ObjectType ? null : _systemConverter.Value;

    public virtual bool CanConvertFrom(Type type)
    {
        // Explicit converters decide their own capabilities; IConvertible alone is not sufficient.
        if (_hasExplicitConverter)
        {
            return SystemConverter.CanConvertFrom(type);
        }

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
        // Do not promise string or numeric conversions that the explicit converter rejects.
        if (_hasExplicitConverter)
        {
            return SystemConverter.CanConvertTo(type);
        }

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
        // Delegate before null, identity and numeric shortcuts: even these inputs may have custom semantics.
        if (_hasExplicitConverter)
        {
            return SystemConverter.ConvertFrom(null, culture, value);
        }

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
        // Explicit conversion failures must propagate instead of falling back to ChangeType or ToString.
        if (_hasExplicitConverter)
        {
            return SystemConverter.ConvertTo(null, culture, value, to);
        }

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