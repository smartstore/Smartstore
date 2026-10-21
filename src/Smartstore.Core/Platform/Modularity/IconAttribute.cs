namespace Smartstore.Engine.Modularity;

/// <summary>
/// Specifies an icon address or concept name for a provider.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class IconAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with an icon address or concept name.
    /// </summary>
    public IconAttribute(string address)
    {
        Guard.NotEmpty(address, nameof(address));
        Address = address;
    }

    /// <summary>
    /// Gets the provider icon address or concept name.
    /// </summary>
    public string Address { get; }
}
