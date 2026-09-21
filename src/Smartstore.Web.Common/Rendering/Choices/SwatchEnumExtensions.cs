using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Rendering.Choices;

public static class SwatchEnumExtensions
{
    /// <summary>
    /// Gets the CSS class suffix for a swatch size, e.g. "lg" for <see cref="SwatchSize.Large"/>.
    /// </summary>
    public static string ToCssToken(this SwatchSize value)
        => value switch
        {
            SwatchSize.XSmall => "xs",
            SwatchSize.Small => "sm",
            SwatchSize.Medium => "md",
            SwatchSize.Large => "lg",
            SwatchSize.XLarge => "xl",
            SwatchSize.XXLarge => "xxl",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    /// <summary>
    /// Gets the CSS class suffix for a swatch shape, e.g. "rounded" for <see cref="SwatchShape.Rounded"/>.
    /// </summary>
    public static string ToCssToken(this SwatchShape value)
        => value switch
        {
            SwatchShape.Rounded => "rounded",
            SwatchShape.Rect => "rect",
            SwatchShape.Circle => "circle",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
