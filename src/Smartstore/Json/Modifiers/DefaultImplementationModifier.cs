#nullable enable

using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Smartstore.ComponentModel;

namespace Smartstore.Json.Modifiers;

internal static class DefaultImplementationModifier
{
    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var baseType = typeInfo.Type;

        if (!baseType.IsInterface && !baseType.IsAbstract)
            return;

        // Don't override an existing factory (e.g. source-gen or custom resolvers).
        if (typeInfo.CreateObject is not null)
            return;

        var implType = DefaultImplementationAttribute.Resolve(baseType);
        if (implType == baseType)
            return;

        ApplyImplTypeConventions(typeInfo, implType);

        typeInfo.CreateObject = () => FastActivator.CreateInstance(implType);
    }

    private static void ApplyImplTypeConventions(JsonTypeInfo baseTypeInfo, Type implType)
    {
        // Only copy already materialized JsonPropertyInfo behaviors from the implementation type.
        // Do not re-evaluate attributes here (that is handled by other modifiers).
        var options = baseTypeInfo.Options;
        if (options is null)
            return;

        JsonTypeInfo? implTypeInfo;
        try
        {
            implTypeInfo = options.GetTypeInfo(implType);
        }
        catch
        {
            return;
        }

        if (implTypeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        // Build a lookup for impl properties by CLR name + property type.
        Dictionary<(string Name, Type Type), JsonPropertyInfo> implProps = [];
        foreach (var p in implTypeInfo.Properties)
        {
            if (p.AttributeProvider is not MemberInfo mi)
                continue;

            implProps[(mi.Name, p.PropertyType)] = p;
        }

        foreach (var baseProp in baseTypeInfo.Properties)
        {
            if (baseProp.AttributeProvider is not MemberInfo baseMi)
                continue;

            if (!implProps.TryGetValue((baseMi.Name, baseProp.PropertyType), out var implProp))
                continue;

            if (implProp.AttributeProvider is not MemberInfo)
                continue;

            // If the implementation type was processed by other modifiers (IgnoreDataMember, DefaultValue, etc.),
            // we can reuse the resulting delegates/flags here.
            // Don't override anything already configured on the base type.

            if (implProp.Get is null)
                baseProp.Get = null;
            if (implProp.Set is null)
                baseProp.Set = null;

            baseProp.ShouldSerialize ??= implProp.ShouldSerialize;
            baseProp.CustomConverter ??= implProp.CustomConverter;

            if (baseProp.IsRequired && !implProp.IsRequired)
                baseProp.IsRequired = false;
        }
    }
}
