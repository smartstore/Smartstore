#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Resolves kit references and serves revisioned sprites on demand.
/// </summary>
public interface IIconKitService
{
    /// <summary>
    /// Gets configured kits without reading SVG drawing data.
    /// </summary>
    IReadOnlyCollection<IconKit> Kits { get; }

    /// <summary>
    /// Gets a sprite reference for a resolved icon, or null when it belongs to no kit.
    /// Shared takes precedence, followed by kit name in ordinal order.
    /// </summary>
    /// <param name="icon">The resolved icon, including its library and variant.</param>
    IconKitReference? GetReference(IconInfo icon);

    /// <summary>
    /// Gets the versioned sprite URL without generating its content.
    /// </summary>
    /// <param name="kitName">The configured kit name.</param>
    /// <param name="library">A library system name or short name, or null for the default.</param>
    /// <param name="variant">A variant name or short name, or null for the default.</param>
    string? GetUrl(string kitName, string? library = null, string? variant = null);

    /// <summary>
    /// Gets the physical path of an immutable sprite file, writing it only on a cache miss.
    /// Existing historical revisions remain available. Returns null for unknown selections
    /// or an unavailable historical revision, which cannot be reconstructed from current sources.
    /// </summary>
    /// <param name="kitName">The configured kit name.</param>
    /// <param name="revision">The required source revision from the sprite URL.</param>
    Task<string?> GetSpriteFileAsync(string kitName, string revision, CancellationToken cancelToken = default);
}

/// <summary>
/// Describes a library-independent group of conceptual icon names.
/// </summary>
public sealed class IconKit
{
    /// <summary>
    /// Creates an immutable kit definition.
    /// </summary>
    /// <param name="name">The configured kit name.</param>
    /// <param name="icons">The conceptual icon names.</param>
    /// <param name="defaultLibrary">The optional default library system name.</param>
    /// <param name="defaultVariant">The optional default variant within the kit default library.</param>
    public IconKit(string name, IEnumerable<string> icons, string? defaultLibrary = null, string? defaultVariant = null)
    {
        Guard.NotEmpty(name);

        Guard.NotNull(icons);

        Name = name;
        Icons = Array.AsReadOnly(icons.ToArray());
        DefaultLibrary = defaultLibrary;
        DefaultVariant = defaultVariant;
    }

    /// <summary>
    /// Gets the configured name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the conceptual names belonging to this kit.
    /// </summary>
    public IReadOnlyList<string> Icons { get; }

    /// <summary>
    /// Gets the optional library system name overriding the global default.
    /// </summary>
    public string? DefaultLibrary { get; }

    /// <summary>
    /// Gets the optional variant override, applicable only to this kit's effective default library.
    /// </summary>
    public string? DefaultVariant { get; }
}

/// <summary>
/// Identifies an external symbol without loading its drawing.
/// </summary>
/// <param name="Href">The versioned application-relative sprite URL including the symbol fragment.</param>
public sealed record IconKitReference(string Href);
