#nullable enable

using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.Json.Modifiers;

internal static class IgnoreDataMemberModifier
{
    internal static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider is not MemberInfo mi)
                continue;

            if (!mi.HasAttribute<IgnoreDataMemberAttribute>(true))
                continue;

            // Ignore on write
            prop.Get = null;

            // Ensure write is blocked even if something later wires a getter again.
            prop.ShouldSerialize ??= static (_, _) => false;

            // Ignore on read
            prop.Set = null;

            // Safety: if it was marked required somewhere, required+no-setter can explode.
            prop.IsRequired = false;
        }
    }
}
