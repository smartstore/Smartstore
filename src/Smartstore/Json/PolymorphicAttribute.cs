namespace Smartstore.Json;

/// <summary>
/// Indicates that the target class, interface, property, or field participates in polymorphic serialization or
/// deserialization.
/// </summary>
/// <remarks>
/// Decorate classes, interfaces, properties, or fields with this attribute to signal that they should be serialized
/// with a $type discriminator pointing to the fully qualified type name. This enables correct deserialization of derived types.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field,
    Inherited = true,
    AllowMultiple = false)]
public sealed class PolymorphicAttribute : Attribute
{
    /// <summary>
    /// If true, array/list values are written as {"$type":"...","$values":[...]}.
    /// If false, array/list values are written raw: [...].
    /// Default is false.
    /// </summary>
    public bool WrapArrays { get; set; }
}
