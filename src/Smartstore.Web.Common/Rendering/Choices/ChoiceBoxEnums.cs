namespace Smartstore.Web.Rendering.Choices;

/// <summary>
/// Size of a choice box.
/// </summary>
public enum ChoiceBoxSize
{
    XSmall = 10,
    Small = 20,
    Medium = 30,
    Large = 40,
    XLarge = 50,
    XXLarge = 60
}

/// <summary>
/// Shape of a choice box.
/// </summary>
public enum ChoiceBoxShape
{
    /// <summary>
    /// Rectangle with rounded corners. The default.
    /// </summary>
    Rounded = 0,

    /// <summary>
    /// Rectangle without corner radius.
    /// </summary>
    Rect = 10,

    /// <summary>
    /// Circle. Only applicable to boxes with a single line of content.
    /// </summary>
    Circle = 20
}

/// <summary>
/// Determines how the price of a choice item is rendered within a choice box.
/// </summary>
public enum ChoiceBoxPriceDisplay
{
    None = 0,
    PriceAdjustment = 10,
    FinalPrice = 20
}

public static class ChoiceBoxEnumExtensions
{
    /// <summary>
    /// Gets the CSS class suffix for a box size, e.g. "lg" for <see cref="ChoiceBoxSize.Large"/>.
    /// </summary>
    public static string ToCssSuffix(this ChoiceBoxSize size)
    {
        return size switch
        {
            ChoiceBoxSize.XSmall => "xs",
            ChoiceBoxSize.Small => "sm",
            ChoiceBoxSize.Large => "lg",
            ChoiceBoxSize.XLarge => "xl",
            ChoiceBoxSize.XXLarge => "xxl",
            _ => "md"
        };
    }

    /// <summary>
    /// Gets the CSS class suffix for a box shape, e.g. "circle" for <see cref="ChoiceBoxShape.Circle"/>.
    /// </summary>
    public static string ToCssSuffix(this ChoiceBoxShape shape)
    {
        return shape switch
        {
            ChoiceBoxShape.Rect => "rect",
            ChoiceBoxShape.Circle => "circle",
            _ => "rounded"
        };
    }
}
