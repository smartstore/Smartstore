#nullable enable

using System.Text.Json;

namespace Smartstore.Json.Polymorphy;

/// <summary>
/// Configuration for NSJ-ish polymorphic serialization using a "$type" discriminator (and optional "$value" wrapper).
/// </summary>
internal sealed class PolymorphyOptions
{
    public static PolymorphyOptions Default => new();
    public static PolymorphyOptions DefaultWithArrays => new() { WrapArrays = true };

    public string TypePropertyName { get; init; } = "$type";

    // STJ-ish wrapper for non-object payloads (arrays/scalars) when WRITING.
    // Legacy NSJ "Objects" won't use this, but our reader supports it.
    public string ScalarValuePropertyName { get; init; } = "$value";

    // Array wrapper name (legacy NSJ TypeNameHandling.All/Arrays)
    public string ArrayValuePropertyName { get; init; } = "$values";

    public bool WrapArrays { get; set; }

    public Func<Type, string?> GetTypeId { get; init; } = static t => t.AssemblyQualifiedNameWithoutVersion();

    public Func<string, Type?> ResolveTypeId { get; init; } = static id =>
        Type.GetType(id, throwOnError: false, ignoreCase: false);

    public Func<Type, bool> IsAllowedType { get; init; } = static t =>
        !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters;

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
