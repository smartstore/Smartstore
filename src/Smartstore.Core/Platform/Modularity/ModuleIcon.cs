namespace Smartstore.Engine.Modularity;

/// <summary>
/// Describes a module or provider icon as either an SVG address or an image URL.
/// </summary>
public sealed record ModuleIcon
{
    /// <summary>
    /// Gets the icon address; null when an image is used.
    /// </summary>
    public string Address { get; init; }

    /// <summary>
    /// Gets the image URL; null when an SVG address is used.
    /// </summary>
    public string ImageUrl { get; init; }
}
