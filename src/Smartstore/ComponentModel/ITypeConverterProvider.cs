#nullable enable

namespace Smartstore.ComponentModel
{
    /// <summary>
    /// Creates <see cref="ITypeConverter"/> instances. Register <see cref="ITypeConverterProvider"/>
    /// instances in <see cref="TypeConverterFactory"/>.
    /// </summary>
    public interface ITypeConverterProvider
    {
        /// <summary>
        /// Creates a <see cref="ITypeConverter"/> for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to create a converter for.</param>
        /// <returns>An <see cref="ITypeConverter"/>.</returns>
        ITypeConverter? GetConverter(Type type);
    }
}
