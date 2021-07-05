using System.Collections.Generic;
using System.Threading.Tasks;
using Smartstore.Core.Checkout.Cart;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute materializer interface.
    /// </summary>
    public partial interface ICheckoutAttributeMaterializer
    {
        /// <summary>
        /// Gets a list of checkout attributes.
        /// </summary>
        /// <param name="selection">Checkout attribute selection.</param>
        /// <returns>List of checkout attributes.</returns>
        Task<List<CheckoutAttribute>> MaterializeCheckoutAttributesAsync(CheckoutAttributeSelection selection);

        /// <summary>
        /// Gets a list of checkout attribute values.
        /// </summary>
        /// <param name="selection">Checkout attribute selection.</param>
        /// <returns>List of checkout attribute values.</returns>
        Task<List<CheckoutAttributeValue>> MaterializeCheckoutAttributeValuesAsync(CheckoutAttributeSelection selection);

        /// <summary>
        /// Gets checkout attributes for a cart.
        /// Checkout attributes which require shippable products are excluded, when <paramref name="cart"/> contains no shippable products.
        /// </summary>
        /// <param name="cart">Shopping cart.</param>
        /// <param name="storeId">Filters checkout attributes by store identifier. 0 to load checkout attributes.</param>
        /// <returns>List of checkout attributes.</returns>
        Task<List<CheckoutAttribute>> GetCheckoutAttributesAsync(IEnumerable<OrganizedShoppingCartItem> cart, int storeId = 0);
    }
}