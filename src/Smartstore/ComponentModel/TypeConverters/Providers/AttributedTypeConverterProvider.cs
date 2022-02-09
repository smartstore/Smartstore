namespace Smartstore.ComponentModel.TypeConverters
{
    public class AttributedTypeConverterProvider : ITypeConverterProvider
    {
        public ITypeConverter GetConverter(Type type)
        {
            var attr = type.GetAttribute<System.ComponentModel.TypeConverterAttribute>(false);
            if (attr != null && attr.ConverterTypeName.HasValue())
            {
                try
                {
                    var converterType = Type.GetType(attr.ConverterTypeName);
                    if (typeof(ITypeConverter).IsAssignableFrom(converterType))
                    {
                        if (!converterType.HasDefaultConstructor())
                        {
                            throw new SmartException("A type converter specified by attribute must have a default parameterless constructor.");
                        }

                        return (ITypeConverter)Activator.CreateInstance(converterType);
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
