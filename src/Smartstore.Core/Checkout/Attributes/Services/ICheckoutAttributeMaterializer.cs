#nullable enable

using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Attributes;

/// <summary>
/// Checkout attribute materializer interface.
/// </summary>
public partial interface ICheckoutAttributeMaterializer
{
    /// <summary>
    /// Materializes the checkout attribute selection for a shopping cart.
    /// </summary>
    /// <param name="cart">Shopping cart.</param>
    /// <param name="matchRules">A value indicating whether to filter checkout attributes by cart rules.</param>
    /// <returns>The materialized checkout attribute selection.</returns>
    Task<MaterializedCheckoutAttributes> MaterializeCheckoutAttributesAsync(ShoppingCart cart, bool matchRules = true);

    /// <summary>
    /// Gets checkout attributes for a cart.
    /// Checkout attributes which require shippable products are excluded, when <paramref name="cart"/> contains no shippable products.
    /// </summary>
    /// <param name="cart">Shopping cart.</param>
    /// <param name="storeId">Filters checkout attributes by store identifier. 0 to load checkout attributes.</param>
    /// <param name="matchRules">A value indicating whether to filter checkout attributes by cart rules.</param>
    /// <returns>List of checkout attributes.</returns>
    Task<List<CheckoutAttribute>> GetCheckoutAttributesAsync(ShoppingCart cart, int storeId = 0, bool matchRules = true);

    /// <summary>
    /// Creates a checkout attribute selection.
    /// </summary>
    /// <param name="query">Product variant query to get the selected checkout attributes from.</param>
    /// <param name="cart">Related shopping cart.</param>
    /// <returns>Checkout attribute selection.</returns>
    Task<CheckoutAttributeSelection> CreateCheckoutAttributeSelectionAsync(ProductVariantQuery query, ShoppingCart cart);
}