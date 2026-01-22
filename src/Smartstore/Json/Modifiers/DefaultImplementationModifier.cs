#nullable enable

using System.Text.Json.Serialization.Metadata;
using Smartstore.ComponentModel;

namespace Smartstore.Json.Modifiers;

internal static class DefaultImplementationModifier
{
    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var declaredType = typeInfo.Type;

        if (!declaredType.IsInterface && !declaredType.IsAbstract)
            return;

        // Don't override an existing factory (e.g. source-gen or custom resolvers).
        if (typeInfo.CreateObject is not null)
            return;

        var implementationType = DefaultImplementationAttribute.Resolve(declaredType);
        if (implementationType == declaredType)
            return;

        typeInfo.CreateObject = () => FastActivator.CreateInstance(implementationType);
    }
}
