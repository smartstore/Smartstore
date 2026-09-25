using System.ComponentModel.DataAnnotations;

namespace Smartstore.Admin.Models.Common;

/// <summary>
/// Represents an editable collection of colors with a permanent primary color.
/// </summary>
public class ColorPaletteModel
{
    /// <summary>
    /// The default maximum number of colors.
    /// </summary>
    public const int DefaultMaxColors = 4;

    /// <summary>
    /// Gets or sets the primary color.
    /// </summary>
    [StringLength(100)]
    public string Color { get; set; }

    /// <summary>
    /// Gets or sets the optional additional colors.
    /// </summary>
    public List<string> AdditionalColors { get; set; } = [];

    /// <summary>
    /// Creates a color palette from a primary and optional additional colors.
    /// </summary>
    public static ColorPaletteModel Create(string color, IEnumerable<string> additionalColors)
    {
        return new()
        {
            Color = color,
            AdditionalColors = additionalColors?.Where(x => x.HasValue()).ToList() ?? []
        };
    }

    /// <summary>
    /// Gets the normalized additional colors up to the configured maximum palette size.
    /// </summary>
    public string[] GetAdditionalColors(int maxColors = DefaultMaxColors)
    {
        var result = (AdditionalColors ?? [])
            .Where(x => x.HasValue())
            .Select(x => x.Trim())
            .Take(Math.Max(0, maxColors - 1))
            .ToArray();

        return result.Length > 0 ? result : null;
    }
}
