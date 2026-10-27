namespace Smartstore.ComponentModel;

/// <summary>
/// Maps a string property containing JSON to or from the matching property's type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonSerializedPropertyAttribute : Attribute
{
}
