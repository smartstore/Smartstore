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
        public required JsonConverterFactory ObjectConverter { get; init; }
        public required JsonConverterFactory ObjectWithArraysConverter { get; init; }
        public required JsonConverterFactory ListConverter { get; init; }
        public required JsonConverterFactory ListWithArraysConverter { get; init; }
        public required JsonConverterFactory DictionaryConverter { get; init; }
        public required JsonConverterFactory DictionaryWithArraysConverter { get; init; }
    }

    private readonly static PolymorphyConverterSet _converterSet = new()
    {
        ObjectConverter = new PolymorphicObjectJsonConverterFactory(PolymorphyOptions.Default),
        ObjectWithArraysConverter = new PolymorphicObjectJsonConverterFactory(PolymorphyOptions.DefaultWithArrays),
        ListConverter = new PolymorphicListJsonConverterFactory(PolymorphyOptions.Default),
        ListWithArraysConverter = new PolymorphicListJsonConverterFactory(PolymorphyOptions.DefaultWithArrays),
        DictionaryConverter = new PolymorphicDictionaryJsonConverterFactory(PolymorphyOptions.Default),
        DictionaryWithArraysConverter = new PolymorphicDictionaryJsonConverterFactory(PolymorphyOptions.DefaultWithArrays)
    };

    /// <summary>
    /// Applies polymorphic JSON converter modifiers to eligible properties of the specified type information.
    /// </summary>
    /// <remarks>This method inspects each property of the provided type and assigns a custom JSON converter
    /// for properties that opt in to polymorphic serialization. Only properties of object, dictionary, or list types
    /// are considered. Properties that do not meet the criteria are left unchanged.</remarks>
    public static void Apply(JsonTypeInfo typeInfo)
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

            prop.CustomConverter = ResolveConverterFactory(kind, wrapArrays);

#if DEBUG
            // Sanity check: if a [Polymorphic] attribute was found, a converter must be assigned.
            // This catches cases where AttributeProvider is missing (e.g. stub/private types) or classification mismatches.
            if (prop.CustomConverter is null)
            {
                var declaring = typeInfo.Type;
                var propName = prop.Name;
                var memberInfo = member?.DeclaringType?.FullName + "." + member?.Name;

                throw new InvalidOperationException(
                    $"[Polymorphic] was detected but no CustomConverter was assigned. " +
                    $"Type='{declaring}', Property='{propName}', PropertyType='{rawPropType}', Member='{memberInfo ?? "<no member info>"}'.");
            }
#endif
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

    public static PolymorphyKind Classify(Type t)
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

            throw new InvalidOperationException("Polymorphic objects must be of type object, interface, abstract class, dictionary with string keys, or list/array.");
        }
    }

    public static JsonConverterFactory ResolveConverterFactory(PolymorphyKind kind, bool wrapArrays)
    {
        return kind switch
        {
            PolymorphyKind.ObjectSlot => wrapArrays
                ? _converterSet.ObjectWithArraysConverter
                : _converterSet.ObjectConverter,

            PolymorphyKind.ListSlot => wrapArrays
                ? _converterSet.ListWithArraysConverter
                : _converterSet.ListConverter,

            PolymorphyKind.DictionarySlot => wrapArrays
                ? _converterSet.DictionaryWithArraysConverter
                : _converterSet.DictionaryConverter,

            _ => throw new InvalidOperationException($"Unsupported polymorphy kind '{kind}'.")
        };
    }

    private static bool IsCandidateType(Type t)
        => PolymorphyCodec.IsPolymorphicType(t);
}
