#nullable enable

using System.ComponentModel;
using System.Globalization;

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Defines a contract for types that can be converted to and from their string representation.
    /// </summary>
    /// <remarks>This interface provides methods for converting an object to its string representation and for
    /// creating an object from a string. Implementations should define the specific rules for these conversions,
    /// including how null or invalid strings are handled.</remarks>
    /// <typeparam name="T">The type that implements this interface, representing the object that can be converted to and from a string.</typeparam>
    public interface IStringBacked<T> where T : struct, IStringBacked<T>
    {
        /// <summary>
        /// Converts the specified string representation to an instance of the type.
        /// </summary>
        /// <param name="value">The string representation to convert. Can be <see langword="null"/>.</param>
        /// <returns>An instance of the type represented by the string, or <see langword="null"/> if the conversion is not possible.</returns>
        static abstract T? FromString(string? value);

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        /// <returns>A string that represents the current object, or <see langword="null"/> if the object does not have a
        /// meaningful string representation.</returns>
        string? ToString();
    }

    /// <summary>
    /// Provides a type converter to convert between strings and a value type <typeparamref name="T"/>  that implements
    /// the <see cref="IStringBacked{T}"/> interface.
    /// </summary>
    public class StringBackedTypeConverter<T> : TypeConverter where T : struct, IStringBacked<T>
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str) return T.FromString(str);
            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is null) return null;
                // Value will normally be a boxed T
                if (value is T t) return t.ToString();
            }

            // Fallback: let base handle unexpected types
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
