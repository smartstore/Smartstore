namespace Smartstore.Web.Rendering;

/// <summary>
/// Prepares color values for the shared swatch presentation.
/// </summary>
public static class SwatchColorHelper
{
    /// <summary>
    /// Gets up to four normalized swatch colors, requiring a visible primary color.
    /// </summary>
    public static string[] GetColors(string primaryColor, IEnumerable<string> additionalColors = null)
    {
        primaryColor = primaryColor?.Trim();
        if (primaryColor.IsEmpty() || primaryColor.EqualsNoCase("transparent"))
        {
            return [];
        }

        return new[] { primaryColor }
            .Concat((additionalColors ?? [])
                .Select(color => color?.Trim())
                .Where(color => color.HasValue() && !color.EqualsNoCase("transparent")))
            .Take(4)
            .ToArray();
    }

    /// <summary>
    /// Determines whether a primary color has additional visible colors.
    /// </summary>
    public static bool HasMultipleColors(string primaryColor, IEnumerable<string> additionalColors)
        => GetColors(primaryColor, additionalColors).Length > 1;

    /// <summary>
    /// Gets a single-color background or the CSS variables for up to four color bands.
    /// </summary>
    public static string GetCss(string primaryColor, IEnumerable<string> additionalColors = null)
    {
        var colors = GetColors(primaryColor, additionalColors);
        if (colors.Length == 0)
        {
            return null;
        }

        if (colors.Length == 1)
        {
            return $"background-color: {colors[0]};";
        }

        return $"--swatch-color-count: {colors.Length};"
            + string.Concat(colors.Select((color, index) => $"--swatch-color-{index + 1}: {color};"));
    }
}
