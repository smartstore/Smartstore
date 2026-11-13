using System.ComponentModel;
using System.Reflection;

namespace Smartstore.ComponentModel.TypeConverters;

public class AttributedTypeConverterProvider : ITypeConverterProvider
{
    public ITypeConverter GetConverter(Type type)
        => GetAttributedConverter(type);

    internal static ITypeConverter GetAttributedConverter(MemberInfo member)
    {
        // Read the attribute once. Properties use Smartstore's hierarchy lookup for
        // overridden or shadowed members; type-level lookup keeps its existing semantics.
        var property = member as PropertyInfo;
        var attr = member.GetAttribute<TypeConverterAttribute>(inherits: property != null);
        if (attr == null)
        {
            return null;
        }

        // Resolve the declared converter directly instead of discovering every property descriptor.
        var converterType = ResolveConverterType(attr.ConverterTypeName, property?.DeclaringType);
        // Prefer Smartstore converters and retain their parameterless-constructor contract.
        if (converterType != null && typeof(ITypeConverter).IsAssignableFrom(converterType))
        {
            var converter = CreateSmartstoreConverter(converterType);
            if (converter != null || property == null)
            {
                return converter;
            }
        }

        // Type-level System converters remain the responsibility of the factory's default fallback.
        if (property == null)
        {
            return null;
        }

        // Use the explicit mode to preserve property semantics without default fast paths.
        // The factory still defers System converter construction until the first conversion request.
        return new DefaultTypeConverter(property.PropertyType, () =>
        {
            if (converterType != null && typeof(TypeConverter).IsAssignableFrom(converterType))
            {
                // Match PropertyDescriptor's constructor preference: converters such as
                // EnumConverter need the property type even when a default constructor exists.
                Type[] parameterTypes = [typeof(Type)];
                if (converterType.GetConstructor(parameterTypes) != null)
                {
                    return (TypeConverter)TypeDescriptor.CreateInstance(null, converterType, parameterTypes, [property.PropertyType]);
                }

                return (TypeConverter)TypeDescriptor.CreateInstance(null, converterType, null, null);
            }

            // Empty, unresolved or unsuitable declarations fall back to the property's type,
            // as PropertyDescriptor does. Valid System converter constructor failures propagate.
            return TypeDescriptor.GetConverter(property.PropertyType);
        });
    }

    private static Type ResolveConverterType(string typeName, Type declaringType)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        try
        {
            // Property attributes may name a converter in the declaring assembly without
            // an assembly-qualified name. Type-level resolution remains unchanged.
            return Type.GetType(typeName, throwOnError: false)
                ?? declaringType?.Assembly.GetType(typeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    private static ITypeConverter CreateSmartstoreConverter(Type converterType)
    {
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