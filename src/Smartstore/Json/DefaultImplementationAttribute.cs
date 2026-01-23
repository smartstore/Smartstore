#nullable enable

using System.Collections.Concurrent;

namespace Smartstore.Json;

/// <summary>
/// Defines the default concrete implementation type for an interface or abstract base type.
/// Intended for deserialization of non-discriminated (non-$type) JSON payloads.
/// Abstract types that are decorated with this attributes are considered non-polymorphic.
/// </summary>
/// <remarks>
/// This attribute is used by a modifier to reflect the implementation type's 
/// JSON property visibility conventions in this abstract type.
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DefaultImplementationAttribute : Attribute
{
    private static readonly ConcurrentDictionary<Type, Type?> _cache = new();

    public DefaultImplementationAttribute(Type implementationType)
    {
        ImplementationType = Guard.NotNull(implementationType);
    }

    public Type ImplementationType { get; }

    public static Type Resolve(Type declaredType)
    {
        Guard.NotNull(declaredType);

        // No need to resolve concrete types.
        if (!declaredType.IsInterface && !declaredType.IsAbstract)
            return declaredType;

        var resolved = _cache.GetOrAdd(declaredType, static t =>
        {
            if (!t.TryGetAttribute<DefaultImplementationAttribute>(inherits: true, out var attr))
                return null;

            var impl = attr.ImplementationType;

            if (impl.IsAbstract || impl.IsInterface)
                throw new InvalidOperationException($"Default implementation type '{impl}' must be concrete.");

            if (!t.IsAssignableFrom(impl))
                throw new InvalidOperationException($"Default implementation type '{impl}' is not assignable to '{t}'.");

            return impl;
        });

        return resolved ?? declaredType;
    }
}