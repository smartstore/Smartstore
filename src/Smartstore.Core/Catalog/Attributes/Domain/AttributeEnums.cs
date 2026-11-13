namespace Smartstore.Core.Catalog.Attributes;

/// <summary>
/// Represents an attribute control type.
/// </summary>
public enum AttributeControlType
{
    /// <summary>
    /// Dropdown list.
    /// </summary>
    DropdownList = 1,

    /// <summary>
    /// Radio list.
    /// </summary>
    RadioList = 2,

    /// <summary>
    /// Checkboxes.
    /// </summary>
    Checkboxes = 3,

    /// <summary>
    /// Text box.
    /// </summary>
    TextBox = 4,

    /// <summary>
    /// Multiline textbox.
    /// </summary>
    MultilineTextbox = 10,

    /// <summary>
    /// Datepicker.
    /// </summary>
    Datepicker = 20,

    /// <summary>
    /// File upload control.
    /// </summary>
    FileUpload = 30,

    /// <summary>
    /// Boxes.
    /// </summary>
    Boxes = 40
}

/// <summary>
/// Represents a value type for product attributes.
/// </summary>
public enum ProductVariantAttributeValueType
{
    /// <summary>
    /// Simple attribute value.
    /// </summary>
    Simple = 0,

    /// <summary>
    /// Linked product attribute value.
    /// </summary>
    ProductLinkage = 10
}

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
