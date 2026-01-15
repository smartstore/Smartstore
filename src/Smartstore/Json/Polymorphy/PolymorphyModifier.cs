#nullable enable

using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Json.Polymorphy;

internal enum PolymorphyKind
{
    None,
    ObjectSlot,
    ListSlot,
    DictionarySlot
}

internal static class PolymorphyModifier
{
    readonly struct PolymorphyConverterSet
    {
        public required JsonConverter ObjectConverter { get; init; }
        public required JsonConverter ObjectWithArraysConverter { get; init; }
        public required JsonConverter ListConverter { get; init; }
        public required JsonConverter ListWithArraysConverter { get; init; }
        public required JsonConverter DictionaryConverter { get; init; }
        public required JsonConverter DictionaryWithArraysConverter { get; init; }
    }

    private readonly static PolymorphyConverterSet _converterSet = new()
    {
        ObjectConverter = new PolymorphicObjectConverterFactory(PolymorphyOptions.Default),
        ObjectWithArraysConverter = new PolymorphicObjectConverterFactory(PolymorphyOptions.DefaultWithArrays),
        ListConverter = new PolymorphicListConverterFactory(PolymorphyOptions.Default),
        ListWithArraysConverter = new PolymorphicListConverterFactory(PolymorphyOptions.DefaultWithArrays),
        DictionaryConverter = new PolymorphicDictionaryConverterFactory(PolymorphyOptions.Default),
        DictionaryWithArraysConverter = new PolymorphicDictionaryConverterFactory(PolymorphyOptions.DefaultWithArrays)
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

            if (!TryGetPolymorphicAttribute(member, propType, out var attr))
                continue;

            // Select converter based on property type
            var kind = Classify(propType);
            var wrapArrays = attr?.WrapArrays ?? false;

            switch (kind)
            {
                case PolymorphyKind.ObjectSlot:
                    prop.CustomConverter = wrapArrays
                        ? _converterSet.ObjectWithArraysConverter
                        : _converterSet.ObjectConverter;
                    break;

                case PolymorphyKind.DictionarySlot:
                    prop.CustomConverter = wrapArrays
                        ? _converterSet.DictionaryWithArraysConverter
                        : _converterSet.DictionaryConverter;
                    break;

                case PolymorphyKind.ListSlot:
                    prop.CustomConverter = wrapArrays
                        ? _converterSet.ListWithArraysConverter
                        : _converterSet.ListConverter;
                    break;

                default:
                    break;
            }
        }
    }

    private static bool TryGetPolymorphicAttribute(MemberInfo? member, Type propertyType, out PolymorphicAttribute? attr)
    {
        // Prefer member-level attribute
        if (member?.TryGetAttribute<PolymorphicAttribute>(true, out attr) == true) 
        {
            return true;
        }

        // Type-level opt-in: "this type itself"
        return propertyType.TryGetAttribute<PolymorphicAttribute>(true, out attr) == true;
    }

    private static PolymorphyKind Classify(Type t)
    {
        if (t.IsDictionaryType(out var keyType, out var valueType))
        {
            if (keyType == typeof(string) && IsCandidateType(valueType))
                return PolymorphyKind.DictionarySlot;

            throw new InvalidOperationException("Polymorphic dictionaries must have string keys and object/interface/abstract value types.");

        }
        else if (t.IsSequenceType(out var elementType))
        {
            if (IsCandidateType(elementType))
                return PolymorphyKind.ListSlot;

            throw new InvalidOperationException("Polymorphic lists must have object/interface/abstract element types.");
        }
        else
        {
            if (IsCandidateType(t))
                return PolymorphyKind.ObjectSlot;

            throw new InvalidOperationException("Polymorphic properties must be of type object, interface, abstract class, dictionary with string keys, or list/array.");
        }
    }

    private static bool IsCandidateType(Type t)
        => PolymorphyCodec.IsPolymorphType(t);
}
