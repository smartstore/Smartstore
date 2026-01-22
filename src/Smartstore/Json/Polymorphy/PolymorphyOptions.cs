#nullable enable

using System.Text.Json;

namespace Smartstore.Json.Polymorphy;

/// <summary>
/// Configuration for NSJ-ish polymorphic serialization using a "$type" discriminator (and optional "$value" wrapper).
/// </summary>
internal sealed class PolymorphyOptions
{
    private readonly static Dictionary<string, string> _legacyTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SmartStore.Web.Framework.Modelling.CustomPropertiesDictionary, SmartStore.Web.Framework"] 
            = "Smartstore.Web.Modelling.CustomPropertiesDictionary, Smartstore.Web.Common"
    };
    
    public static PolymorphyOptions Default => new();
    public static PolymorphyOptions DefaultWithArrays => new() { WrapArrays = true };

    public string TypePropertyName { get; init; } = "$type";

    // STJ-ish wrapper for non-object payloads (arrays/scalars) when WRITING.
    // Legacy NSJ "Objects" won't use this, but our reader supports it.
    public string ScalarValuePropertyName { get; init; } = "$value";

    // Array wrapper name (legacy NSJ TypeNameHandling.All/Arrays)
    public string ArrayValuePropertyName { get; init; } = "$values";

    public bool WrapArrays { get; set; }

    public Func<Type, string?> GetTypeId { get; init; } = static t 
        => t.AssemblyQualifiedNameWithoutVersion();

    public Func<string, Type?> ResolveTypeId { get; init; } = static id =>
    { 
        var type = Type.GetType(id, throwOnError: false, ignoreCase: false);
        if (type == null && _legacyTypeMap.TryGetValue(id, out var mappedId))
        {
            type = Type.GetType(mappedId, throwOnError: false, ignoreCase: false);
        }

        return type;
    };

    public Func<Type, bool> IsAllowedType { get; init; } = static t
        => !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters;

    public string GetRequiredTypeId(Type runtimeType)
        => GetTypeId(runtimeType) ?? throw new JsonException($"Cannot create discriminator for '{runtimeType}'.");

    public Type ResolveRequiredType(string typeId)
    {
        var t = ResolveTypeId(typeId) ?? throw new JsonException($"Unknown discriminator '{typeId}'.");
        if (!IsAllowedType(t))
            throw new JsonException($"Type '{t}' is not allowed.");

        return t;
    }
}
