#nullable enable

namespace Smartstore.Core.Checkout.Attributes;

/// <summary>
/// Checkout Attributes formatting helper.
/// </summary>
public partial interface ICheckoutAttributeFormatter
{
    /// <summary>
    /// Formats selected checkout attributes.
    /// </summary>
    /// <param name="attributes">Materialized checkout attribute selection.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>Formatted checkout attributes.</returns>
    Task<string> FormatAttributesAsync(MaterializedCheckoutAttributes attributes, CheckoutAttributeFormatOptions? options = null);
}