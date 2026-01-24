namespace Smartstore.ComponentModel.TypeConverters;

public class AttributedTypeConverterProvider : ITypeConverterProvider
{
    public ITypeConverter GetConverter(Type type)
    {
        var attr = type.GetAttribute<System.ComponentModel.TypeConverterAttribute>(inherits: false);
        var typeName = attr?.ConverterTypeName;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        Type converterType;

        try
        {
            converterType = Type.GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }

        if (converterType is null || !typeof(ITypeConverter).IsAssignableFrom(converterType))
        {
            return null;
        }

        if (!converterType.HasDefaultConstructor())
        {
            throw new InvalidOperationException(
                "A type converter specified by attribute must have a default parameterless constructor.");
        }

        try
        {
            return (ITypeConverter)Activator.CreateInstance(converterType);
        }
        catch
        {
            return null;
        }
    }
}
