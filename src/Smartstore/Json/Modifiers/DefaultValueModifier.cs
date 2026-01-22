#nullable enable

using System.Collections;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using Smartstore.Domain;

namespace Smartstore.Json.Modifiers;

internal static class DefaultValueModifier
{
    internal static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var p in typeInfo.Properties)
        {
            if (p.AttributeProvider?.TryGetAttribute<DefaultValueAttribute>(true, out var attr) ?? false)
            {
                var isSequenceType = p.PropertyType.IsSequenceType();
                var isDefaultable = typeof(IDefaultable).IsAssignableFrom(p.PropertyType);
                if (isSequenceType || isDefaultable)
                {
                    if (Equals(attr.Value, "[]"))
                    {
                        if (isSequenceType)
                            // Ignore empty lists/arrays/dictionaries when default is "[]"
                            p.ShouldSerialize = (o, value) => !ShouldIgnoreEmptySequence(value as IEnumerable);
                        else
                            // Ignore objects in default/initial state when default is "[]"
                            p.ShouldSerialize = (o, value) => !ShouldIgnoreDefaultState(value as IDefaultable);
                    }
                }
                else
                {
                    var defaultValue = attr.Value.Convert(p.PropertyType);
                    var dv = defaultValue;
                    p.ShouldSerialize = (o, value) => !ShouldIgnoreDefaultValue(value, dv);
                }
            }
        }
    }

    private static bool ShouldIgnoreDefaultValue(object? value, object? defaultValue)
        => value == null || Equals(value, defaultValue);

    private static bool ShouldIgnoreEmptySequence(IEnumerable? value)
        => value == null || !value.GetEnumerator().MoveNext();

    private static bool ShouldIgnoreDefaultState(IDefaultable? value)
        => value == null || value.IsDefaultState;
}
