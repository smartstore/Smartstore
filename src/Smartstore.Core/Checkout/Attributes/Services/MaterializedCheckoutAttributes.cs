#nullable enable

using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Attributes;

/// <summary>
/// Represents a checkout attribute selection together with the attributes and values available in its cart context.
/// </summary>
public sealed class MaterializedCheckoutAttributes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaterializedCheckoutAttributes"/> class.
    /// </summary>
    /// <param name="customer">Customer associated with the selection.</param>
    /// <param name="selection">Checkout attribute selection.</param>
    /// <param name="attributes">Checkout attributes available in the cart context.</param>
    /// <param name="selectedValues">Selected checkout attribute values available in the cart context.</param>
    public MaterializedCheckoutAttributes(
        Customer customer,
        CheckoutAttributeSelection selection,
        IReadOnlyList<CheckoutAttribute> attributes,
        IReadOnlyList<CheckoutAttributeValue> selectedValues)
    {
        Customer = Guard.NotNull(customer);
        Selection = Guard.NotNull(selection);
        Attributes = Guard.NotNull(attributes);
        SelectedValues = Guard.NotNull(selectedValues);
    }

    /// <summary>
    /// Gets the customer associated with the selection.
    /// </summary>
    public Customer Customer { get; }

    /// <summary>
    /// Gets the checkout attribute selection.
    /// </summary>
    public CheckoutAttributeSelection Selection { get; }

    /// <summary>
    /// Gets the checkout attributes available in the cart context.
    /// </summary>
    public IReadOnlyList<CheckoutAttribute> Attributes { get; }

    /// <summary>
    /// Gets the selected checkout attribute values available in the cart context.
    /// </summary>
    public IReadOnlyList<CheckoutAttributeValue> SelectedValues { get; }
}
