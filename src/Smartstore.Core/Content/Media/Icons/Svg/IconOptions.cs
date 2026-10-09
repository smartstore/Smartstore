#nullable enable

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Supplies DOM presentation independently of library and variant selection.
/// Sizing, animation and transforms use the existing icon classes and CSS variables.
/// </summary>
public sealed class IconOptions : ICloneable<IconOptions>
{
    private Dictionary<string, string> _attributes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a predefined size: 2xs, xs, sm, lg, xl, 2xl, or 1x through 10x.
    /// </summary>
    public string? Size { get; set; }

    /// <summary>
    /// Gets or sets a custom size multiplier taking precedence over Size.
    /// </summary>
    public float? FontScale { get; set; }

    /// <summary>
    /// Gets or sets whether the icon occupies a fixed-width slot.
    /// </summary>
    public bool FixedWidth { get; set; }

    /// <summary>
    /// Gets or sets a CSS color. Omission preserves inherited color.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets whether the inverse foreground color is used.
    /// </summary>
    public bool Inverse { get; set; }

    /// <summary>
    /// Gets or sets an animation: spin, pulse, spin-pulse, beat, fade, throb, cylon,
    /// cylon-vertical, bounce, beat-fade, flip, or shake.
    /// </summary>
    public string? Animation { get; set; }

    /// <summary>
    /// Gets or sets a CSS animation duration, such as 800ms or 2s.
    /// </summary>
    public string? AnimationDuration { get; set; }

    /// <summary>
    /// Gets or sets reverse playback. Omission preserves the animation default.
    /// </summary>
    public bool? AnimationReverse { get; set; }

    /// <summary>
    /// Gets or sets clockwise degrees, replacing the address or mapping rotation. Zero resets it.
    /// </summary>
    public int? Rotate { get; set; }

    /// <summary>
    /// Gets or sets horizontal mirroring, replacing the address or mapping value.
    /// </summary>
    public bool? FlipHorizontal { get; set; }

    /// <summary>
    /// Gets or sets vertical mirroring, replacing the address or mapping value.
    /// </summary>
    public bool? FlipVertical { get; set; }

    /// <summary>
    /// Gets or sets the drawing scale without changing its layout size.
    /// </summary>
    public float? Scale { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset in sixteenths of an em. Positive values move right.
    /// </summary>
    public float? ShiftX { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset in sixteenths of an em. Positive values move down.
    /// </summary>
    public float? ShiftY { get; set; }

    /// <summary>
    /// Gets or sets a positive stroke multiplier, replacing the address or mapping multiplier.
    /// </summary>
    public double? StrokeScale { get; set; }

    /// <summary>
    /// Gets caller attributes, including class, style and accessible labels.
    /// Structural attributes and the resolved identity are owned by the renderer.
    /// </summary>
    public IDictionary<string, string> Attributes => _attributes;

    public IconOptions Clone()
    {
        // Other fields contain only value types and immutable strings. Detach the
        // mutable dictionary after copying fields, preserving its key comparer.
        var clone = (IconOptions)MemberwiseClone();
        clone._attributes = new Dictionary<string, string>(_attributes, _attributes.Comparer);
        return clone;
    }

    object ICloneable.Clone() => Clone();
}
