namespace Smartstore.Core.Configuration;

/// <summary>
/// Indicates that a setting property is global and cannot be overridden on a per-store basis.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class GlobalSettingAttribute : Attribute
{
}
