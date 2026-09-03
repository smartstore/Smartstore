namespace Smartstore.Web.Rendering.Choices;

public enum SwatchSize
{
    XSmall = 0,
    Small = 10,
    Medium = 20,
    Large = 30,
    XLarge = 40,
    XXLarge = 50
}

public enum SwatchShape
{
    Rounded = 0,
    Rect = 10,
    Circle = 20
}

/// <summary>
/// Specifies which price information is displayed in a swatch.
/// </summary>
public enum SwatchPriceDisplayMode
{
    /// <summary>
    /// Does not display price information.
    /// </summary>
    None,

    /// <summary>
    /// Displays the signed price adjustment of the attribute value.
    /// </summary>
    Adjustment,

    /// <summary>
    /// Displays the calculated final price for the candidate selection.
    /// </summary>
    FinalPrice
}

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
