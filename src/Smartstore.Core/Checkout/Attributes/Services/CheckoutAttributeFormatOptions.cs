#nullable enable

namespace Smartstore.Core.Checkout.Attributes;

/// <summary>
/// Specifies options for formatting checkout attributes.
/// </summary>
public record CheckoutAttributeFormatOptions
{
    /// <summary>
    /// Gets the default formatting options.
    /// </summary>
    public static CheckoutAttributeFormatOptions Default { get; } = new();

    /// <summary>
    /// Gets the separator between formatted attributes.
    /// </summary>
    public string? ItemSeparator { get; init; } = "<br />";

    /// <summary>
    /// Gets a value indicating whether values should be HTML encoded.
    /// </summary>
    public bool HtmlEncode { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether prices should be included.
    /// </summary>
    public bool IncludePrices { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether HTML hyperlinks should be included.
    /// </summary>
    public bool IncludeHyperlinks { get; init; } = true;
}
