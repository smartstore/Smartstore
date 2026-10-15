#nullable enable

namespace Smartstore.IconKit;

/// <summary>
/// Supplies rendering context and caller-owned HTML attributes independently of icon presentation.
/// </summary>
public sealed class IconOptions : ICloneable<IconOptions>
{
    /// <summary>
    /// Gets or sets the rendering area; null uses the caller's area.
    /// </summary>
    public string? Area { get; set; }

    /// <summary>
    /// Gets or sets the requested name or address retained in placeholder diagnostics.
    /// </summary>
    public string? RequestedName { get; set; }

    /// <summary>
    /// Gets or sets whether to embed the drawing even when a kit sprite is available.
    /// </summary>
    public bool ForceInline { get; set; }

    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets caller attributes, including class, style and accessible labels.
    /// Structural attributes and the resolved identity are owned by the renderer.
    /// </summary>
    public IDictionary<string, string?> Attributes => _attributes;

    /// <summary>
    /// Gets the current class attribute or appends CSS classes to it.
    /// Null and empty values leave existing classes unchanged.
    /// </summary>
    public string? CssClass
    {
        get => _attributes.GetValueOrDefault("class");
        set
        {
            if (value != null)
            {
                Attributes.AppendInValue("class", ' ', value);
            }
        }
    }

    public IconOptions Clone()
    {
        // Other fields contain only value types and immutable strings. Detach the
        // mutable dictionary after copying fields, preserving its key comparer.
        var clone = (IconOptions)MemberwiseClone();
        clone._attributes = new Dictionary<string, string?>(_attributes, _attributes.Comparer);
        return clone;
    }

    object ICloneable.Clone() => Clone();
}
