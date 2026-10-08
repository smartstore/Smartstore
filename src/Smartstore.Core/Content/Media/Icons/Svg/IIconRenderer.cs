#nullable enable

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Renders resolved icons without exposing the inline or sprite choice to callers.
/// </summary>
public interface IIconRenderer
{
    /// <summary>
    /// Creates a complete SVG element, or null if the source is no longer available.
    /// </summary>
    /// <param name="icon">The resolved icon identity.</param>
    /// <param name="options">Caller-owned DOM attributes and presentation options.</param>
    Task<TagBuilder?> RenderAsync(IconInfo icon, IconOptions? options = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Creates an HTML stack host for independently rendered icon layers.
    /// </summary>
    /// <param name="content">The already rendered icon children.</param>
    /// <param name="options">Stack presentation and caller-owned DOM attributes. StrokeScale applies to individual icons only.</param>
    TagBuilder RenderStack(IHtmlContent content, IconOptions? options = null);
}

/// <summary>
/// Supplies DOM presentation independently of library and variant selection.
/// Sizing, animation and transforms use the existing icon classes and CSS variables.
/// </summary>
public sealed class IconOptions
{
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
    public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
