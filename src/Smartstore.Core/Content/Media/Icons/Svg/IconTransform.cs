using System.Globalization;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Immutable mapping transformation, applied before caller presentation and never baked into the source cache.
/// </summary>
public readonly record struct IconTransform
{
    /// <summary>
    /// Creates a transformation that flips first and then rotates around the viewBox center.
    /// </summary>
    /// <param name="flipX">Whether to mirror horizontally.</param>
    /// <param name="flipY">Whether to mirror vertically.</param>
    /// <param name="rotation">The clockwise rotation in degrees.</param>
    public IconTransform(bool flipX, bool flipY, double rotation)
    {
        if (!double.IsFinite(rotation))
        {
            throw new ArgumentOutOfRangeException(nameof(rotation));
        }

        FlipX = flipX;
        FlipY = flipY;
        Rotation = (rotation % 360 + 360) % 360;
    }

    /// <summary>
    /// Gets whether the horizontal axis is mirrored.
    /// </summary>
    public bool FlipX { get; }

    /// <summary>
    /// Gets whether the vertical axis is mirrored.
    /// </summary>
    public bool FlipY { get; }

    /// <summary>
    /// Gets clockwise degrees normalized to the range [0, 360).
    /// </summary>
    public double Rotation { get; }

    /// <summary>
    /// Gets whether this value leaves the source unchanged.
    /// </summary>
    public bool IsIdentity => !FlipX && !FlipY && Rotation == 0;

    /// <summary>
    /// Formats an SVG transform around the actual coordinate rectangle, including its origin offset.
    /// </summary>
    /// <param name="viewBox">The source or fallback viewBox used to calculate the transform center.</param>
    internal string ToSvg(string viewBox)
    {
        var parts = viewBox.Split([' ', ',', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var x = double.Parse(parts[0], CultureInfo.InvariantCulture) + double.Parse(parts[2], CultureInfo.InvariantCulture) / 2;
        var y = double.Parse(parts[1], CultureInfo.InvariantCulture) + double.Parse(parts[3], CultureInfo.InvariantCulture) / 2;
        // SVG evaluates this list right-to-left: move to origin, flip, rotate, move back.
        return FormattableString.Invariant($"translate({x:R} {y:R}) rotate({Rotation:R}) scale({(FlipX ? -1 : 1)} {(FlipY ? -1 : 1)}) translate({-x:R} {-y:R})");
    }

    /// <summary>
    /// Gets a culture-independent representation for sprite revision fingerprints.
    /// </summary>
    internal string RevisionKey => FormattableString.Invariant($"{FlipX}:{FlipY}:{Rotation:R}");
}

/// <summary>
/// Stores a parsed mapping once per library generation instead of parsing it for each rendering.
/// </summary>
/// <param name="Name">The actual unqualified icon name.</param>
/// <param name="Transform">The mapping's normalized transformation.</param>
/// <param name="StrokeScale">The additional per-icon stroke multiplier.</param>
internal sealed record IconMapping(string Name, IconTransform Transform, double StrokeScale = 1)
{
    /// <summary>
    /// Parses a mapping target and its optional presentation modifiers.
    /// </summary>
    /// <param name="value">The unqualified target and optional query.</param>
    /// <param name="location">The mapping location used for diagnostics.</param>
    internal static IconMapping Parse(string value, string location)
    {
        var separator = value?.IndexOf('?') ?? -1;
        var name = separator < 0 ? value : value[..separator];
        if (!IconAddress.IsName(name))
        {
            throw new InvalidDataException($"Invalid icon mapping target in {location}.");
        }

        var modifiers = separator < 0 ? default : IconModifiers.Parse(value.AsSpan(separator + 1), location);
        return new IconMapping(name, modifiers.Apply(default), modifiers.StrokeScale ?? 1);
    }
}
