using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Smartstore.ComponentModel;

/// <summary>
/// Provides helper methods for applying data contract attributes to JSON type metadata, enabling compatibility between
/// data contract serialization conventions and System.Text.Json serialization.
/// </summary>
public static class DataContractModifiers
{
    /// <summary>
    /// Configures the specified type to ignore properties marked with the IgnoreDataMemberAttribute during JSON
    /// serialization and deserialization.
    /// </summary>
    /// <remarks>This method disables both reading and writing for properties decorated with
    /// IgnoreDataMemberAttribute, ensuring they are not included in JSON output or processed during deserialization.
    /// Properties marked as required will also be unset to prevent errors when setters are removed.</remarks>
    /// <param name="typeInfo">The type metadata to apply ignore rules to. Must represent an object type; properties with the
    /// IgnoreDataMemberAttribute will be excluded from serialization and deserialization.</param>
    public static void ApplyIgnoreDataMember(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider is not MemberInfo mi)
                continue;

            if (!mi.IsDefined(typeof(IgnoreDataMemberAttribute), inherit: true))
                continue;

            // Ignore on write
            prop.Get = null;

            // Ignore on read
            prop.Set = null;

            // Safety: if it was marked required somewhere, required+no-setter can explode.
            prop.IsRequired = false;

            //prop.ShouldSerialize = (_, __) => false;
            //prop.ShouldDeserialize = (_, __) => false;
        }
    }

    /// <summary>
    /// Applies the DataMemberAttribute name and order values to the properties of the specified JsonTypeInfo object if
    /// it represents an object type.
    /// </summary>
    /// <remarks>This method sets the Name and Order of each property in the JsonTypeInfo to match the
    /// corresponding values from the DataMemberAttribute, if present. Only properties backed by a MemberInfo and
    /// decorated with DataMemberAttribute are affected. Properties without DataMemberAttribute or with negative Order
    /// values are left unchanged.</remarks>
    /// <param name="typeInfo">The JsonTypeInfo instance whose properties will be updated based on DataMemberAttribute metadata. Must represent
    /// an object type; otherwise, no changes are made.</param>
    public static void ApplyDataMemberNameAndOrder(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var prop in typeInfo.Properties)
        {
            if (prop.AttributeProvider is not MemberInfo mi)
                continue;

            var dm = mi.GetCustomAttribute<DataMemberAttribute>(inherit: true);
            if (dm is null)
                continue;

            if (!string.IsNullOrWhiteSpace(dm.Name))
            {
                // JsonPropertyInfo.Name is settable
                prop.Name = dm.Name;
            }

            if (dm.Order >= 0)
            {
                // JsonPropertyInfo.Order is settable
                prop.Order = dm.Order;
            }
        }
    }
}
