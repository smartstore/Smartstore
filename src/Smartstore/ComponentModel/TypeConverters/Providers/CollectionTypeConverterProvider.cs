namespace Smartstore.ComponentModel.TypeConverters
{
    public class CollectionTypeConverterProvider : ITypeConverterProvider
    {
        public ITypeConverter GetConverter(Type type)
        {
            if (type.IsEnumerableType(out var elementType))
            {
                var converter = (ITypeConverter)Activator.CreateInstance(typeof(EnumerableConverter<>).MakeGenericType(elementType), type);
                return converter;
            }

            return null;
        }
    }
}
