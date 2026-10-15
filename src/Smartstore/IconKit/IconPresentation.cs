#nullable enable

using System.Globalization;
using Smartstore.Utilities;

namespace Smartstore.IconKit;

/// <summary>
/// Immutable presentation values shared by individual icons and stack hosts.
/// Omitted values remain distinct from explicit resets.
/// </summary>
public readonly record struct IconPresentation
{
    /// <summary>
    /// Gets or sets a predefined size: 2xs, xs, sm, lg, xl, 2xl, or 1x through 10x.
    /// </summary>
    public string? Size { get; init; }

    /// <summary>
    /// Gets or sets a custom size multiplier taking precedence over Size.
    /// </summary>
    public float? FontScale { get; init; }

    /// <summary>
    /// Gets or sets whether the icon occupies a fixed-width slot.
    /// </summary>
    public bool? FixedWidth { get; init; }

    /// <summary>
    /// Gets or sets a CSS color. Omission preserves inherited color.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets or sets whether the inverse foreground color is used.
    /// </summary>
    public bool? Inverse { get; init; }

    /// <summary>
    /// Gets or sets an animation: spin, pulse, beat, fade, throb, bounce, flip, shake,
    /// spin-snap, swing, or wag.
    /// </summary>
    public string? Animation { get; init; }

    /// <summary>
    /// Gets or sets a CSS animation duration, such as 800ms or 2s.
    /// </summary>
    public string? AnimationDuration { get; init; }

    /// <summary>
    /// Gets or sets reverse playback. Omission preserves the animation default.
    /// </summary>
    public bool? AnimationReverse { get; init; }

    /// <summary>
    /// Gets or sets clockwise degrees, replacing the address or mapping rotation. Zero resets it.
    /// </summary>
    public double? Rotate { get; init; }

    /// <summary>
    /// Gets or sets horizontal mirroring, replacing the address or mapping value.
    /// </summary>
    public bool? FlipHorizontal { get; init; }

    /// <summary>
    /// Gets or sets vertical mirroring, replacing the address or mapping value.
    /// </summary>
    public bool? FlipVertical { get; init; }

    /// <summary>
    /// Gets or sets whether to mirror horizontally in RTL. Null preserves the concept policy.
    /// </summary>
    public bool? MirrorRtl { get; init; }

    /// <summary>
    /// Gets or sets the drawing scale without changing its layout size.
    /// </summary>
    public float? Scale { get; init; }

    /// <summary>
    /// Gets or sets the horizontal offset in sixteenths of an em. Positive values move right.
    /// </summary>
    public float? ShiftX { get; init; }

    /// <summary>
    /// Gets or sets the vertical offset in sixteenths of an em. Positive values move down.
    /// </summary>
    public float? ShiftY { get; init; }

    /// <summary>
    /// Gets or sets a positive stroke multiplier, replacing the address or mapping multiplier.
    /// </summary>
    public double? StrokeScale { get; init; }

    /// <summary>
    /// Fills omitted presentation fields from a lower-priority presentation.
    /// </summary>
    /// <param name="fallback">The defaults to use only for omitted fields.</param>
    public IconPresentation WithDefaults(IconPresentation fallback) => this with
    {
        Size = Size ?? fallback.Size,
        FontScale = FontScale ?? fallback.FontScale,
        FixedWidth = FixedWidth ?? fallback.FixedWidth,
        Color = Color ?? fallback.Color,
        Inverse = Inverse ?? fallback.Inverse,
        Animation = Animation ?? fallback.Animation,
        AnimationDuration = AnimationDuration ?? fallback.AnimationDuration,
        AnimationReverse = AnimationReverse ?? fallback.AnimationReverse,
        Rotate = Rotate ?? fallback.Rotate,
        FlipHorizontal = FlipHorizontal ?? fallback.FlipHorizontal,
        FlipVertical = FlipVertical ?? fallback.FlipVertical,
        MirrorRtl = MirrorRtl ?? fallback.MirrorRtl,
        Scale = Scale ?? fallback.Scale,
        ShiftX = ShiftX ?? fallback.ShiftX,
        ShiftY = ShiftY ?? fallback.ShiftY,
        StrokeScale = StrokeScale ?? fallback.StrokeScale,
    };

    internal IconTransform Apply(IconTransform fallback)
        => new(FlipHorizontal ?? fallback.FlipX, FlipVertical ?? fallback.FlipY, Rotate ?? fallback.Rotation);

    internal static bool IsValidSize(ReadOnlySpan<char> value)
        => value is "2xs" or "xs" or "sm" or "lg" or "xl" or "2xl"
            or "1x" or "2x" or "3x" or "4x" or "5x" or "6x" or "7x" or "8x" or "9x" or "10x";

    internal static bool IsValidAnimation(ReadOnlySpan<char> value)
        => value is "spin" or "pulse" or "beat" or "fade" or "throb" or "bounce"
            or "flip" or "shake" or "spin-snap" or "swing" or "wag";

    internal void Validate()
    {
        static bool Finite(double? value) => !value.HasValue || double.IsFinite(value.Value);
        static bool Positive(double? value) => !value.HasValue || (double.IsFinite(value.Value) && value.Value > 0);
        static bool CssValue(string? value) => value == null || (!string.IsNullOrWhiteSpace(value) && value.IndexOfAny([';', '{', '}']) < 0);

        if ((Size != null && !IsValidSize(Size))
            || (Animation != null && !IsValidAnimation(Animation))
            || !CssValue(Color) || !CssValue(AnimationDuration)
            || !Positive(FontScale) || !Positive(Scale) || !Positive(StrokeScale)
            || !Finite(Rotate) || !Finite(ShiftX) || !Finite(ShiftY))
        {
            throw new InvalidDataException("Invalid icon presentation value.");
        }
    }

    /// <summary>
    /// Parses presentation modifiers with or without a leading question mark.
    /// Empty input produces an empty presentation. Invalid known values are ignored;
    /// the last valid value wins. Unknown keys and unsafe CSS delimiters are rejected.
    /// </summary>
    /// <param name="value">The query-like presentation string.</param>
    public static IconPresentation Parse(ReadOnlySpan<char> value)
    {
        if (!TryParse(value, out var presentation))
        {
            throw new InvalidDataException($"Invalid icon presentation '{value.ToString()}'.");
        }
        return presentation;
    }

    /// <summary>
    /// Attempts to parse presentation modifiers with or without a leading question mark.
    /// Empty input succeeds; invalid input returns false and an empty presentation.
    /// </summary>
    /// <param name="value">The query-like presentation string.</param>
    /// <param name="presentation">The parsed value on success.</param>
    public static bool TryParse(ReadOnlySpan<char> value, out IconPresentation presentation)
        => TryParseModifiers(value.StartsWith("?") ? value[1..] : value, out presentation, tolerant: true);

    /// <summary>
    /// Formats a culture-independent query with a leading question mark, or an empty string
    /// when no values are set. Reserved delimiters are escaped; CSS colors remain readable.
    /// </summary>
    public override string ToString()
    {
        if (this == default) return string.Empty;
        Validate();
        using var lease = StringBuilderPool.Instance.Get(out var result);
        void Add(string key, string? value)
        {
            if (value == null) return;
            result.Append(result.Length == 0 ? '?' : '&').Append(key).Append('=');
            // Append unescaped spans directly; only query delimiters need escaping, not a color's #.
            var remaining = value.AsSpan();
            int index;
            while ((index = remaining.IndexOfAny("%&=?")) >= 0)
            {
                result.Append(remaining[..index]);
                result.Append(remaining[index] switch { '%' => "%25", '&' => "%26", '=' => "%3D", _ => "%3F" });
                remaining = remaining[(index + 1)..];
            }
            result.Append(remaining);
        }

        void AddFlag(string key, bool? value)
        {
            if (!value.HasValue) return;
            result.Append(result.Length == 0 ? '?' : '&').Append(key);
            if (!value.Value) result.Append("=false");
        }

        Add("size", Size);
        Add("font-scale", FontScale?.ToString("R", CultureInfo.InvariantCulture));
        AddFlag("fw", FixedWidth);
        Add("color", Color);
        AddFlag("inverse", Inverse);
        Add("anim", Animation);
        Add("anim-dur", AnimationDuration);
        AddFlag("anim-rev", AnimationReverse);
        Add("rotate", Rotate?.ToString("R", CultureInfo.InvariantCulture));
        AddFlag("mirror-rtl", MirrorRtl);
        Add("scale", Scale?.ToString("R", CultureInfo.InvariantCulture));
        Add("shift-x", ShiftX?.ToString("R", CultureInfo.InvariantCulture));
        Add("shift-y", ShiftY?.ToString("R", CultureInfo.InvariantCulture));
        Add("stroke-scale", StrokeScale?.ToString("R", CultureInfo.InvariantCulture));
        if (FlipHorizontal.HasValue && FlipVertical.HasValue)
        {
            Add("flip", FlipHorizontal.Value ? (FlipVertical.Value ? "xy" : "x") : (FlipVertical.Value ? "y" : "none"));
        }
        else
        {
            AddFlag("flip-h", FlipHorizontal);
            AddFlag("flip-v", FlipVertical);
        }

        return result.ToString();
    }

    internal static IconPresentation ParseModifiers(ReadOnlySpan<char> query, string location, bool tolerant = false)
    {
        if (!TryParseModifiers(query, out var result, tolerant))
        {
            throw new InvalidDataException($"Invalid or duplicate icon modifier in {location}.");
        }
        return result;
    }

    private static bool TryParseBoolean(ReadOnlySpan<char> value, out bool result)
    {
        if (value is "1" or "0")
        {
            result = value[0] == '1';
            return true;
        }
        return bool.TryParse(value, out result);
    }

    private static bool TryParseModifiers(ReadOnlySpan<char> query, out IconPresentation result, bool tolerant)
    {
        result = default;
        var seen = 0;
        foreach (var range in query.Split('&'))
        {
            var pair = query[range];
            if (tolerant && pair.IsWhiteSpace()) continue;
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            key = key switch
            {
                "fw" => "fixed-width",
                "anim" => "animation",
                "anim-dur" => "animation-duration",
                "anim-rev" => "animation-reverse",
                _ => key
            };
            var isBoolean = key is "fixed-width" or "inverse" or "animation-reverse" or "flip-h" or "flip-v" or "mirror-rtl";
            var value = separator < 0 && isBoolean ? "true".AsSpan() : separator < 0 ? default : pair[(separator + 1)..];
            // Numeric and Boolean modifiers normally stay entirely on spans.
            if (value.Contains('%')) value = Uri.UnescapeDataString(value.ToString()).AsSpan();
            var flag = key switch
            {
                "size" => 1,
                "font-scale" => 2,
                "fixed-width" => 4,
                "color" => 8,
                "inverse" => 16,
                "animation" => 32,
                "animation-duration" => 64,
                "animation-reverse" => 128,
                "rotate" => 256,
                "flip-h" => 512,
                "flip-v" => 1024,
                "mirror-rtl" => 2048,
                "scale" => 4096,
                "shift-x" => 8192,
                "shift-y" => 16384,
                "stroke-scale" => 32768,
                "flip" => 1536,
                _ => 0
            };
            if (flag == 0
                || (!tolerant && ((seen & flag) != 0 || value.IsWhiteSpace()))
                || ((key is "color" or "animation-duration") && value.IndexOfAny([';', '{', '}']) >= 0))
            {
                result = default;
                return false;
            }
            if (tolerant && value.IsWhiteSpace()) continue;
            seen |= flag;
            switch (key)
            {
                case "flip" when value is "x" or "y" or "xy" or "none":
                    result = result with { FlipHorizontal = value.Contains('x'), FlipVertical = value.Contains('y') };
                    break;
                case "size" when IconPresentation.IsValidSize(value):
                    result = result with { Size = value.ToString() };
                    break;
                case "font-scale" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fontScaleValue) && float.IsFinite(fontScaleValue) && fontScaleValue > 0:
                    result = result with { FontScale = fontScaleValue };
                    break;
                case "fixed-width" when TryParseBoolean(value, out var fixedWidthValue):
                    result = result with { FixedWidth = fixedWidthValue };
                    break;
                case "color" when value.IndexOfAny([';', '{', '}']) < 0:
                    result = result with { Color = value.ToString() };
                    break;
                case "inverse" when TryParseBoolean(value, out var inverseValue):
                    result = result with { Inverse = inverseValue };
                    break;
                case "animation" when IconPresentation.IsValidAnimation(value):
                    result = result with { Animation = value.ToString() };
                    break;
                case "animation-duration" when value.IndexOfAny([';', '{', '}']) < 0:
                    result = result with { AnimationDuration = value.ToString() };
                    break;
                case "animation-reverse" when TryParseBoolean(value, out var animationReverseValue):
                    result = result with { AnimationReverse = animationReverseValue };
                    break;
                case "rotate" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rotateValue) && double.IsFinite(rotateValue):
                    result = result with { Rotate = rotateValue };
                    break;
                case "flip-h" when TryParseBoolean(value, out var flipHorizontalValue):
                    result = result with { FlipHorizontal = flipHorizontalValue };
                    break;
                case "flip-v" when TryParseBoolean(value, out var flipVerticalValue):
                    result = result with { FlipVertical = flipVerticalValue };
                    break;
                case "mirror-rtl" when TryParseBoolean(value, out var mirrorRtlValue):
                    result = result with { MirrorRtl = mirrorRtlValue };
                    break;
                case "scale" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var scaleValue) && float.IsFinite(scaleValue) && scaleValue > 0:
                    result = result with { Scale = scaleValue };
                    break;
                case "shift-x" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var shiftXValue) && float.IsFinite(shiftXValue):
                    result = result with { ShiftX = shiftXValue };
                    break;
                case "shift-y" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var shiftYValue) && float.IsFinite(shiftYValue):
                    result = result with { ShiftY = shiftYValue };
                    break;
                case "stroke-scale" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var strokeScaleValue) && double.IsFinite(strokeScaleValue) && strokeScaleValue > 0:
                    result = result with { StrokeScale = strokeScaleValue };
                    break;
                default:
                    if (tolerant) break;
                    result = default;
                    return false;
            }
        }
        return true;
    }
}
