#nullable enable

using System.Globalization;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Retains omitted modifier values so callers can replace individual mapping defaults.
/// </summary>
internal readonly record struct IconModifiers
{
    /// <summary>
    /// Gets the optional horizontal flip override.
    /// </summary>
    internal bool? FlipX { get; init; }

    /// <summary>
    /// Gets the optional vertical flip override.
    /// </summary>
    internal bool? FlipY { get; init; }

    /// <summary>
    /// Gets the optional clockwise rotation override.
    /// </summary>
    internal double? Rotation { get; init; }

    /// <summary>
    /// Gets the optional stroke multiplier override.
    /// </summary>
    internal double? StrokeScale { get; init; }

    /// <summary>
    /// Replaces only explicitly supplied transform components.
    /// </summary>
    /// <param name="fallback">The lower-priority transformation.</param>
    internal IconTransform Apply(IconTransform fallback)
        => new(FlipX ?? fallback.FlipX, FlipY ?? fallback.FlipY, Rotation ?? fallback.Rotation);

    /// <summary>
    /// Formats an explicit source address, optionally retaining its resolved presentation modifiers.
    /// </summary>
    /// <param name="icon">The resolved source and presentation.</param>
    /// <param name="includeModifiers">Whether to include transforms and the stroke multiplier.</param>
    internal static string FormatAddress(IconInfo icon, bool includeModifiers = false)
    {
        var address = new IconAddress(icon.Name, icon.LibraryKey, icon.VariantKey, skipMapping: true).ToString();
        if (!includeModifiers)
        {
            return address;
        }

        var flip = icon.Transform.FlipX ? (icon.Transform.FlipY ? "xy" : "x") : (icon.Transform.FlipY ? "y" : "none");
        return address + "?flip=" + flip
            + "&rotate=" + icon.Transform.Rotation.ToString("R", CultureInfo.InvariantCulture)
            + "&stroke-scale=" + icon.StrokeScale.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a modifier query without allocating parameter arrays or a duplicate-key set.
    /// </summary>
    /// <param name="query">The query following the question mark.</param>
    /// <param name="location">The address or mapping location used for diagnostics.</param>
    internal static IconModifiers Parse(ReadOnlySpan<char> query, string location)
    {
        var result = new IconModifiers();
        var seen = 0;
        foreach (var range in query.Split('&'))
        {
            var pair = query[range];
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? default : pair[(separator + 1)..];
            var flag = key switch { "flip" => 1, "rotate" => 2, "stroke-scale" => 4, _ => 0 };
            if (flag == 0 || (seen & flag) != 0 || value.IsEmpty)
            {
                throw new InvalidDataException($"Invalid or duplicate icon modifier '{pair.ToString()}' in {location}.");
            }

            seen |= flag;
            if (flag == 1 && value is "x" or "y" or "xy" or "none")
            {
                result = result with { FlipX = value.Contains('x'), FlipY = value.Contains('y') };
            }
            else if (flag != 1 && double.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) && (flag != 4 || number > 0))
            {
                result = flag == 2 ? result with { Rotation = number } : result with { StrokeScale = number };
            }
            else
            {
                throw new InvalidDataException($"Invalid icon modifier '{pair.ToString()}' in {location}.");
            }
        }

        return result;
    }
}
