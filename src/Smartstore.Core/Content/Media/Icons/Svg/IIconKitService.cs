#nullable enable

using System.Collections.ObjectModel;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Resolves kit references and serves revisioned sprites on demand.
/// </summary>
public interface IIconKitService
{
    /// <summary>
    /// Gets the current revisioned browser manifest URL, including the request path base.
    /// </summary>
    string GetManifestUrl();

    /// <summary>
    /// Gets the physical manifest file, publishing it on demand. Unknown historical revisions return null.
    /// </summary>
    /// <param name="revision">The manifest content fingerprint.</param>
    Task<string?> GetManifestFileAsync(string revision, CancellationToken cancelToken = default);

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
/// Describes a group of conceptual names with optional concrete source overrides.
/// </summary>
public sealed class IconKit
{
    /// <summary>
    /// Creates an immutable kit definition.
    /// </summary>
    /// <param name="name">The configured kit name.</param>
    /// <param name="icons">The complete list of unique, unqualified conceptual names.</param>
    /// <param name="defaultLibrary">The optional default library system name.</param>
    /// <param name="defaultVariant">The optional default variant within the kit default library.</param>
    /// <param name="sources">Optional concrete source addresses keyed by names present in <paramref name="icons"/>. Source names are not mapped again.</param>
    /// <param name="icon">The optional icon address representing this kit in pickers. Null uses the generic kit icon.</param>
    public IconKit(string name, IEnumerable<string> icons, string? defaultLibrary = null, string? defaultVariant = null,
        IReadOnlyDictionary<string, string>? sources = null, string? icon = null)
    {
        Guard.NotEmpty(name);

        Guard.NotNull(icons);

        Name = name;
        Icons = Array.AsReadOnly(icons.ToArray());
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in Icons)
        {
            if (!IconAddress.IsName(value) || !names.Add(value))
            {
                throw new InvalidDataException($"Invalid or duplicate icon name '{value}' in kit '{name}'.");
            }
        }

        var addresses = new Dictionary<string, IconAddress>(StringComparer.Ordinal);
        var sourceValues = new Dictionary<string, string>(StringComparer.Ordinal);
        if (sources != null)
        {
            foreach (var pair in sources)
            {
                if (!names.Contains(pair.Key) || !IconAddress.TryParse(pair.Value, out var address))
                {
                    throw new InvalidDataException($"Invalid source '{pair.Key}' in kit '{name}'. Source keys must belong to icons and values must be icon addresses.");
                }

                sourceValues.Add(pair.Key, pair.Value);
                addresses.Add(pair.Key, address);
            }
        }

        Sources = new ReadOnlyDictionary<string, string>(sourceValues);
        SourceAddresses = addresses;
        DefaultLibrary = defaultLibrary;
        DefaultVariant = defaultVariant;
        Icon = icon;
    }

    /// <summary>
    /// Gets the configured name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional icon address representing this kit in pickers. It does not add a kit member.
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// Gets the complete list of conceptual names belonging to this kit.
    /// </summary>
    public IReadOnlyList<string> Icons { get; }

    /// <summary>
    /// Gets concrete source overrides keyed by kit concept. An override never adds a kit member.
    /// </summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>
    /// Gets source addresses parsed once per definition, keyed by the public concept and symbol name.
    /// </summary>
    internal IReadOnlyDictionary<string, IconAddress> SourceAddresses { get; }

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
