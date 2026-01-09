#nullable enable

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Smartstore.Json.Converters;

namespace Smartstore.Json;

internal static class PolymorphyModifier
{
    
    private readonly static PolymorphyConverterSet _converterSet = new()
    {
        ObjectConverter = new PolymorphicObjectConverterFactory(PolymorphyOptions.Default),
        ListConverter = null!,
        DictionaryConverter = new PolymorphicDictionaryConverterFactory(PolymorphyOptions.Default)
    };

    /// <summary>
    /// Applies polymorphic JSON converter modifiers to eligible properties of the specified type information.
    /// </summary>
    /// <remarks>This method inspects each property of the provided type and assigns a custom JSON converter
    /// for properties that opt in to polymorphic serialization. Only properties of object, dictionary, or list types
    /// are considered. Properties that do not meet the criteria are left unchanged.</remarks>
    public static void ApplyPolymorphyModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var prop in typeInfo.Properties)
        {
            // MemberInfo exists for reflection-based resolver (DefaultJsonTypeInfoResolver).
            var member = prop.AttributeProvider as MemberInfo;

            var rawPropType = prop.PropertyType;
            var propType = rawPropType.GetNonNullableType();

            if (!IsPolymorphyOptIn(member, propType))
                continue;

            // Select converter based on property type
            var kind = Classify(propType);

            switch (kind)
            {
                case PolymorphyKind.ObjectSlot:
                    prop.CustomConverter = _converterSet.ObjectConverter;
                    break;

                case PolymorphyKind.DictionarySlot:
                    prop.CustomConverter = _converterSet.DictionaryConverter;
                    break;

                case PolymorphyKind.ListSlot:
                    prop.CustomConverter = _converterSet.ListConverter;
                    break;

                default:
                    break;
            }
        }
    }

    private static bool IsPolymorphyOptIn(MemberInfo? member, Type propertyType)
    {
        // Member-level opt-in
        if (member?.HasAttribute<PolymorphicAttribute>(true) == true)
            return true;

        // Type-level opt-in: "this type itself"
        if (propertyType.HasAttribute<PolymorphicAttribute>(true))
            return true;

        return false;
    }

    private static PolymorphyKind Classify(Type t)
    {
        if (t.IsDictionaryType(out var keyType, out var valueType))
        {
            if (keyType == typeof(string) && IsCandidateType(valueType))
            {
                return PolymorphyKind.DictionarySlot;
            }
            else
            {
                throw new InvalidOperationException("Polymorphic dictionaries must have string keys and object/interface/abstract value types.");
            }
        }
        else if (t.IsSequenceType(out var elementType))
        {
            if (IsCandidateType(elementType))
            {
                return PolymorphyKind.ListSlot;
            }
            else
            {
                throw new InvalidOperationException("Polymorphic lists must have object/interface/abstract element types.");
            }
        }
        else
        {
            if (IsCandidateType(t))
            {
                return PolymorphyKind.ObjectSlot;
            }
            else
            {
                throw new InvalidOperationException("Polymorphic properties must be of type object, interface, abstract class, dictionary with string keys, or list/array.");
            }
        }
    }

    private static bool IsCandidateType(Type t)
        => t == typeof(object) || t.IsInterface || t.IsAbstract;

    readonly struct PolymorphyConverterSet
    {
        public required JsonConverter ObjectConverter { get; init; }
        public required JsonConverter ListConverter { get; init; }
        public required JsonConverter DictionaryConverter { get; init; }
    }

    enum PolymorphyKind
    {
        None,
        ObjectSlot,
        ListSlot,
        DictionarySlot
    }
}
